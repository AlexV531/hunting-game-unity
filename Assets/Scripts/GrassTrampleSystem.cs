using UnityEngine;
using System.Collections.Generic;

public class GrassTrampleSystem : MonoBehaviour
{
    [Header("Trample Settings")]
    [SerializeField] private float trampleRadius = 1.5f;
    [SerializeField] private float trampleStrength = 1.0f;
    [SerializeField] private float recoverySpeed = 0.5f;
    [SerializeField] private int maxActiveTramplers = 10;

    [Header("Trail Settings")]
    [SerializeField] private bool enableTrails = true;
    [SerializeField] private float trailLifetime = 180f; // 3 minutes
    [SerializeField] private float trailFadeTime = 5f;   // fade duration
    [SerializeField] private float trailSpacing = 0.4f;
    [SerializeField] private int maxTrailPoints = 2000;

    private static GrassTrampleSystem instance;
    private List<TrampleData> activeTramplers = new List<TrampleData>();
    private List<TrailPoint> trailPoints = new List<TrailPoint>();

    private ComputeBuffer trailBuffer;

    private class TrampleData
    {
        public Transform transform;
        public float currentStrength;
        public float targetStrength;
        public Vector3 lastTrailPosition;
        public float distanceSinceLastTrail;
    }

    private class TrailPoint
    {
        public Vector3 position;
        public float strength;
        public float creationTime;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;

            trailBuffer = new ComputeBuffer(maxTrailPoints, sizeof(float) * 4);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (trailBuffer != null)
        {
            trailBuffer.Release();
            trailBuffer = null;
        }
    }

    void Update()
    {
        if (!enableTrails) return;

        UpdateTrails();
        UpdateShaderBuffer();
    }

    public static void RegisterTrampler(Transform trampler)
    {
        if (instance == null) return;
        if (instance.activeTramplers.Count >= instance.maxActiveTramplers) return;

        var data = new TrampleData
        {
            transform = trampler,
            currentStrength = 0f,
            targetStrength = instance.trampleStrength,
            lastTrailPosition = trampler.position,
            distanceSinceLastTrail = 0f
        };
        instance.activeTramplers.Add(data);
    }

    public static void UnregisterTrampler(Transform trampler)
    {
        if (instance == null) return;
        instance.activeTramplers.RemoveAll(t => t.transform == trampler);
    }

    private void UpdateTrails()
    {
        // Update existing tramplers
        foreach (var trampler in activeTramplers)
        {
            if (trampler.transform == null) continue;

            Vector3 currentPos = trampler.transform.position;
            float distance = Vector3.Distance(currentPos, trampler.lastTrailPosition);
            trampler.distanceSinceLastTrail += distance;

            if (trampler.distanceSinceLastTrail >= trailSpacing)
            {
                // Add new trail point
                if (trailPoints.Count >= maxTrailPoints)
                    trailPoints.RemoveAt(0); // remove oldest

                trailPoints.Add(new TrailPoint
                {
                    position = currentPos,
                    strength = trampler.targetStrength,
                    creationTime = Time.time
                });

                trampler.distanceSinceLastTrail = 0f;
            }

            trampler.lastTrailPosition = currentPos;
        }

        // Remove old trails beyond lifetime
        trailPoints.RemoveAll(t => Time.time - t.creationTime > trailLifetime + trailFadeTime);
    }

    private void UpdateShaderBuffer()
    {
        int count = trailPoints.Count;
        if (count > maxTrailPoints) count = maxTrailPoints;

        Vector4[] bufferData = new Vector4[maxTrailPoints];
        for (int i = 0; i < count; i++)
        {
            var t = trailPoints[i];
            bufferData[i] = new Vector4(t.position.x, t.position.y, t.position.z, t.creationTime);
        }

        // Fill remaining slots with zero
        for (int i = count; i < maxTrailPoints; i++)
            bufferData[i] = Vector4.zero;

        trailBuffer.SetData(bufferData);

        Shader.SetGlobalBuffer("_TrampleTrailBuffer", trailBuffer);
        Shader.SetGlobalInt("_TrampleTrailCount", trailPoints.Count);
        Shader.SetGlobalFloat("_TrampleRadius", trampleRadius);
        Shader.SetGlobalFloat("_RecoverySpeed", recoverySpeed);
        Shader.SetGlobalFloat("_TrailLifetime", trailLifetime);
        Shader.SetGlobalFloat("_FadeTime", trailFadeTime);
        Shader.SetGlobalFloat("_GlobalTime", Time.time);
    }
}
