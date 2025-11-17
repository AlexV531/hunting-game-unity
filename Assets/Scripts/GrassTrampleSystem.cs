using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class GrassTrampleSystem : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private Vector2 terrainWorldMin = new Vector2(0, 0);
    [SerializeField] private Vector2 terrainWorldSize = new Vector2(2000, 2000);
    
    [Header("RenderTexture Settings")]
    [SerializeField] [Tooltip("512=Fast, 1024=Balanced, 2048=High Quality")] 
    private int textureResolution = 2048;
    [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;
    
    [Header("Trample Settings")]
    [SerializeField] private float trampleStrength = 1.5f;
    [SerializeField] private float decayRate = 0.5f;
    [SerializeField] private int maxActiveTramplers = 10;

    [Header("Trail Settings")]
    [SerializeField] private bool enableTrails = true;
    [SerializeField] private float trailSpacing = 1f;
    [SerializeField] private float footprintSize = 0.8f;

    [Header("Performance")]
    [SerializeField] [Tooltip("Decay every N frames (1=smoothest, 2-3=recommended, 5+=fastest)")]
    private int decayUpdateRate = 3;
    
    [Header("Shaders - REQUIRED FOR BUILD")]
    [SerializeField] private Shader decayShader;
    [SerializeField] private Shader stampShader;
    
    private int frameCounter = 0;
    private static GrassTrampleSystem instance;
    private List<TrampleData> activeTramplers = new List<TrampleData>();

    private RenderTexture trampleMap;
    private RenderTexture tempMap;
    private Material decayMaterial;
    private Material stampMaterial;
    private CommandBuffer commandBuffer;

    private class TrampleData
    {
        public Transform transform;
        public Vector3 lastTrailPosition;
        public float distanceSinceLastTrail;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeRenderTextures();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (trampleMap != null) trampleMap.Release();
        if (tempMap != null) tempMap.Release();
        if (decayMaterial != null) Destroy(decayMaterial);
        if (stampMaterial != null) Destroy(stampMaterial);
        if (commandBuffer != null) commandBuffer.Release();
    }

    private void InitializeRenderTextures()
    {
        Debug.Log("[GrassTrampleSystem] Initializing RenderTextures...");
        
        // Create render textures
        trampleMap = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
        trampleMap.filterMode = filterMode;
        trampleMap.wrapMode = TextureWrapMode.Clamp;
        trampleMap.Create();
        Debug.Log($"[GrassTrampleSystem] Created trampleMap: {textureResolution}x{textureResolution}");

        tempMap = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
        tempMap.filterMode = filterMode;
        tempMap.wrapMode = TextureWrapMode.Clamp;
        tempMap.Create();

        // Clear to black
        commandBuffer = new CommandBuffer();
        commandBuffer.name = "TrampleSystem";
        commandBuffer.SetRenderTarget(trampleMap);
        commandBuffer.ClearRenderTarget(false, true, Color.clear);
        Graphics.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();

        // Create materials - use serialized shader references
        if (decayShader == null)
        {
            Debug.LogError("[GrassTrampleSystem] Decay shader not assigned! Assign it in the inspector.");
            decayShader = Shader.Find("Hidden/GrassTrampleDecay");
        }
        
        if (stampShader == null)
        {
            Debug.LogError("[GrassTrampleSystem] Stamp shader not assigned! Assign it in the inspector.");
            stampShader = Shader.Find("Hidden/GrassTrampleStamp");
        }
        
        if (decayShader == null || stampShader == null)
        {
            Debug.LogError("[GrassTrampleSystem] CRITICAL: Shaders missing! System will not work.");
            enabled = false;
            return;
        }
        
        decayMaterial = new Material(decayShader);
        stampMaterial = new Material(stampShader);

        // Set global shader properties
        Shader.SetGlobalTexture("_TrampleMap", trampleMap);
        Shader.SetGlobalVector("_GridWorldMin", new Vector4(terrainWorldMin.x, 0, terrainWorldMin.y, 0));
        Shader.SetGlobalVector("_GridWorldSize", new Vector4(terrainWorldSize.x, 0, terrainWorldSize.y, 0));
        
        Debug.Log($"[GrassTrampleSystem] Terrain bounds: Min({terrainWorldMin.x}, {terrainWorldMin.y}) Size({terrainWorldSize.x}, {terrainWorldSize.y})");
        Debug.Log("[GrassTrampleSystem] Initialization complete!");
    }

    void Update()
    {
        if (!enableTrails) return;

        frameCounter++;
        
        if (frameCounter % decayUpdateRate == 0)
        {
            ApplyDecay();
        }
        
        UpdateTramplers();
        
        Shader.SetGlobalFloat("_GlobalTime", Time.time);
    }

    private void ApplyDecay()
    {
        if (decayMaterial == null) return;
        
        float decayFactor = 1f - (decayRate * Time.deltaTime * decayUpdateRate);
        decayMaterial.SetFloat("_DecayFactor", Mathf.Clamp01(decayFactor));
        
        Graphics.Blit(trampleMap, tempMap, decayMaterial);
        Graphics.Blit(tempMap, trampleMap);
    }

    private void UpdateTramplers()
    {
        foreach (var trampler in activeTramplers)
        {
            if (trampler.transform == null) continue;

            Vector3 currentPos = trampler.transform.position;
            float distance = Vector3.Distance(currentPos, trampler.lastTrailPosition);
            trampler.distanceSinceLastTrail += distance;

            if (trampler.distanceSinceLastTrail >= trailSpacing)
            {
                StampFootprint(currentPos, trampler.transform.forward);
                trampler.distanceSinceLastTrail = 0f;
            }

            trampler.lastTrailPosition = currentPos;
        }
    }

    private void StampFootprint(Vector3 worldPosition, Vector3 direction)
    {
        if (stampMaterial == null) return;
        
        Vector2 uv = WorldToUV(new Vector2(worldPosition.x, worldPosition.z));
                
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
        {
            Debug.LogWarning($"[GrassTrampleSystem] Stamp UV out of bounds at {worldPosition}");
            return;
        }
        
        Vector2 dirNormalized = new Vector2(direction.x, direction.z).normalized;
        Vector2 dirEncoded = dirNormalized * 0.5f + Vector2.one * 0.5f;

        stampMaterial.SetVector("_StampCenter", new Vector4(uv.x, uv.y, 0, 0));
        stampMaterial.SetFloat("_StampRadius", footprintSize / terrainWorldSize.x);
        stampMaterial.SetFloat("_StampStrength", trampleStrength);
        stampMaterial.SetVector("_StampDirection", new Vector4(dirEncoded.x, dirEncoded.y, 0, 0));

        Graphics.Blit(trampleMap, tempMap, stampMaterial);
        Graphics.Blit(tempMap, trampleMap);
    }

    private Vector2 WorldToUV(Vector2 worldPosXZ)
    {
        return new Vector2(
            (worldPosXZ.x - terrainWorldMin.x) / terrainWorldSize.x,
            (worldPosXZ.y - terrainWorldMin.y) / terrainWorldSize.y
        );
    }

    public static void RegisterTrampler(Transform trampler)
    {
        if (instance == null) return;
        if (instance.activeTramplers.Count >= instance.maxActiveTramplers) return;
        if (trampler == null) return;

        var data = new TrampleData
        {
            transform = trampler,
            lastTrailPosition = trampler.position,
            distanceSinceLastTrail = 0f
        };
        instance.activeTramplers.Add(data);
        
        Debug.Log($"[GrassTrampleSystem] Registered trampler: {trampler.name}");
    }

    public static void UnregisterTrampler(Transform trampler)
    {
        if (instance == null) return;
        instance.activeTramplers.RemoveAll(t => t.transform == trampler);
    }
}
