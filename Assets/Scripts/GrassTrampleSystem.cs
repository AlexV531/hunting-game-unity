using UnityEngine;
using System.Collections.Generic;

public class GrassTrampleSystem : MonoBehaviour
{
    [Header("Trample Settings")]
    [SerializeField] private float trampleRadius = 1.5f;
    [SerializeField] private float trailLifetime = 5.0f; // seconds
    [SerializeField] private int maxActiveTramplers = 10;

    [Header("Trail Settings")]
    [SerializeField] private bool enableTrails = true;
    [SerializeField] private float trailSpacing = 0.4f;
    [SerializeField] private int maxTrailPoints = 2000; // Max points in ComputeBuffer

    [Header("Optimization")]
    [SerializeField] private bool useCameraCulling = true;
    [SerializeField] private float maxTrailDistance = 50.0f;

    private Camera mainCamera;
    private static GrassTrampleSystem instance;

    private List<Trampler> activeTramplers = new List<Trampler>();
    private List<TrailPoint> trailPoints = new List<TrailPoint>();

    private ComputeBuffer trailBuffer;

    private class Trampler
    {
        public Transform transform;
        public Vector3 lastTrailPosition;
        public float distanceSinceLastTrail;
    }

    private class TrailPoint
    {
        public Vector3 position;
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

    void Start()
    {
        mainCamera = Camera.main;
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
        if (enableTrails)
            UpdateTrails();

        UpdateShaderProperties();
    }

    public static void RegisterTrampler(Transform trampler)
    {
        if (instance == null) return;
        if (instance.activeTramplers.Count >= instance.maxActiveTramplers) return;

        instance.activeTramplers.Add(new Trampler
        {
            transform = trampler,
            lastTrailPosition = trampler.position,
            distanceSinceLastTrail = 0f
        });
    }

    public static void UnregisterTrampler(Transform trampler)
    {
        if (instance == null) return;
        instance.activeTramplers.RemoveAll(t => t.transform == trampler);
    }

    private void UpdateTrails()
    {
        float time = Time.time;

        // Remove old trails
        trailPoints.RemoveAll(t => (time - t.creationTime) > trailLifetime);

        foreach (var trampler in activeTramplers)
        {
            if (trampler.transform == null) continue;

            Vector3 currentPos = trampler.transform.position;
            trampler.distanceSinceLastTrail += Vector3.Distance(currentPos, trampler.lastTrailPosition);

            if (trampler.distanceSinceLastTrail >= trailSpacing)
            {
                if (trailPoints.Count < maxTrailPoints)
                {
                    trailPoints.Add(new TrailPoint
                    {
                        position = currentPos,
                        creationTime = time
                    });
                }
                else
                {
                    // Overwrite oldest trail point
                    trailPoints[0] = new TrailPoint
                    {
                        position = currentPos,
                        creationTime = time
                    };
                }

                trampler.distanceSinceLastTrail = 0f;
                trampler.lastTrailPosition = currentPos;
            }
        }
    }

    private void UpdateShaderProperties()
    {
        int count = trailPoints.Count;
        Vector4[] bufferData = new Vector4[maxTrailPoints];

        for (int i = 0; i < count; i++)
        {
            var t = trailPoints[i];
            bufferData[i] = new Vector4(t.position.x, t.position.y, t.position.z, t.creationTime);
        }

        // Fill remaining slots with zeros
        for (int i = count; i < maxTrailPoints; i++)
            bufferData[i] = Vector4.zero;

        trailBuffer.SetData(bufferData);

        Shader.SetGlobalBuffer("_TrampleTrailBuffer", trailBuffer);
        Shader.SetGlobalInt("_TrampleTrailCount", count);
        Shader.SetGlobalFloat("_TrampleRadius", trampleRadius);
        Shader.SetGlobalFloat("_TrailLifetime", trailLifetime);
        Shader.SetGlobalFloat("_GlobalTime", Time.time);
    }

    void OnDrawGizmos()
    {
        if (!enableTrails || trailPoints == null) return;

        Gizmos.color = Color.red;
        foreach (var t in trailPoints)
        {
            Gizmos.DrawSphere(t.position, trampleRadius * 0.5f);
        }
    }
}
