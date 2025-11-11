using UnityEngine;
using System.Collections.Generic;

public class GrassTrampleSystem : MonoBehaviour
{
    [Header("Trample Settings")]
    [SerializeField] private float trampleRadius = 1.5f;
    [SerializeField] private float trampleStrength = 1.0f;
    [SerializeField] private float recoverySpeed = 0.5f;
    [SerializeField] private int maxTramplers = 20;
    
    [Header("Trail Settings")]
    [SerializeField] private bool enableTrails = true;
    [SerializeField] private float trailLifetime = 5.0f; // How long trails persist
    [SerializeField] private float trailSpacing = 0.3f; // Distance between trail points
    [SerializeField] private int maxTrailPoints = 100; // Max total trail points
    
    private static GrassTrampleSystem instance;
    private List<TrampleData> activeTramplers = new List<TrampleData>();
    private List<TrailPoint> trailPoints = new List<TrailPoint>();
    private Vector4[] positionBuffer;
    private float[] strengthBuffer;
    
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
        public float currentStrength;
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            positionBuffer = new Vector4[maxTramplers];
            strengthBuffer = new float[maxTramplers];
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (enableTrails)
        {
            UpdateTrails();
        }
        UpdateShaderProperties();
    }
    
    public static void RegisterTrampler(Transform trampler)
    {
        if (instance == null) return;
        
        if (instance.activeTramplers.Count >= instance.maxTramplers)
        {
            Debug.LogWarning("Max tramplers reached!");
            return;
        }
        
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
        // Update existing trail points (fade out over time)
        for (int i = trailPoints.Count - 1; i >= 0; i--)
        {
            var trail = trailPoints[i];
            float age = Time.time - trail.creationTime;
            
            if (age >= trailLifetime)
            {
                // Remove expired trails
                trailPoints.RemoveAt(i);
            }
            else
            {
                // Fade out strength over lifetime
                float lifeProgress = age / trailLifetime;
                trail.currentStrength = Mathf.Lerp(trail.strength, 0f, lifeProgress);
            }
        }
        
        // Create new trail points from moving tramplers
        foreach (var trampler in activeTramplers)
        {
            if (trampler.transform == null) continue;
            
            Vector3 currentPos = trampler.transform.position;
            float distance = Vector3.Distance(currentPos, trampler.lastTrailPosition);
            trampler.distanceSinceLastTrail += distance;
            
            // Create trail point if moved far enough
            if (trampler.distanceSinceLastTrail >= trailSpacing)
            {
                // Don't exceed max trail points
                if (trailPoints.Count < maxTrailPoints)
                {
                    trailPoints.Add(new TrailPoint
                    {
                        position = currentPos,
                        strength = trampler.targetStrength,
                        creationTime = Time.time,
                        currentStrength = trampler.targetStrength
                    });
                }
                else
                {
                    // Remove oldest trail point to make room
                    trailPoints.RemoveAt(0);
                    trailPoints.Add(new TrailPoint
                    {
                        position = currentPos,
                        strength = trampler.targetStrength,
                        creationTime = Time.time,
                        currentStrength = trampler.targetStrength
                    });
                }
                
                trampler.distanceSinceLastTrail = 0f;
            }
            
            trampler.lastTrailPosition = currentPos;
        }
    }
    
    private void UpdateShaderProperties()
    {
        // Clean up null references
        activeTramplers.RemoveAll(t => t.transform == null);
        
        int totalPoints = 0;
        
        // First, add active tramplers (these have priority)
        for (int i = 0; i < activeTramplers.Count && totalPoints < maxTramplers; i++)
        {
            var trampler = activeTramplers[i];
            Vector3 pos = trampler.transform.position;
            positionBuffer[totalPoints] = new Vector4(pos.x, pos.y, pos.z, 1f);
            
            // Smoothly interpolate strength
            trampler.currentStrength = Mathf.Lerp(
                trampler.currentStrength, 
                trampler.targetStrength, 
                Time.deltaTime * 5f
            );
            strengthBuffer[totalPoints] = trampler.currentStrength;
            totalPoints++;
        }
        
        // Then add trail points if trails are enabled
        if (enableTrails)
        {
            for (int i = 0; i < trailPoints.Count && totalPoints < maxTramplers; i++)
            {
                var trail = trailPoints[i];
                positionBuffer[totalPoints] = new Vector4(trail.position.x, trail.position.y, trail.position.z, 1f);
                strengthBuffer[totalPoints] = trail.currentStrength;
                totalPoints++;
            }
        }
        
        // Fill remaining slots with zeros
        for (int i = totalPoints; i < maxTramplers; i++)
        {
            positionBuffer[i] = Vector4.zero;
            strengthBuffer[i] = 0f;
        }
        
        // Send to shader
        Shader.SetGlobalVectorArray("_TramplerPositions", positionBuffer);
        Shader.SetGlobalFloatArray("_TramplerStrengths", strengthBuffer);
        Shader.SetGlobalInt("_TramplerCount", totalPoints);
        Shader.SetGlobalFloat("_TrampleRadius", trampleRadius);
        Shader.SetGlobalFloat("_RecoverySpeed", recoverySpeed);
        Shader.SetGlobalFloat("_GlobalTime", Time.time);
    }
    
    void OnDrawGizmos()
    {
        if (!enableTrails || trailPoints == null) return;
        
        // Visualize trail points
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        foreach (var trail in trailPoints)
        {
            float alpha = trail.currentStrength / trampleStrength;
            Gizmos.color = new Color(1f, 0.5f, 0f, alpha * 0.5f);
            Gizmos.DrawSphere(trail.position, trampleRadius * 0.5f);
        }
    }
}
