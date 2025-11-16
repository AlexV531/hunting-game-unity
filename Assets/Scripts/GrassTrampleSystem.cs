using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class GrassTrampleSystem : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private Vector2 terrainWorldMin = new Vector2(0, 0); // Bottom-left corner (UPDATED for 0,0 to 2000,2000 terrain)
    [SerializeField] private Vector2 terrainWorldSize = new Vector2(2000, 2000);  // 2km x 2km
    
    [Header("RenderTexture Settings")]
    [SerializeField] [Tooltip("512=Fast, 1024=Balanced, 2048=High Quality")] 
    private int textureResolution = 2048; // High quality default
    [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;
    
    [Header("Trample Settings")]
    // [SerializeField] private float trampleRadius = 1.5f;
    [SerializeField] private float trampleStrength = 5.0f;
    [SerializeField] private float decayRate = 0.5f; // How fast trampling fades per second
    [SerializeField] private int maxActiveTramplers = 10;

    [Header("Trail Settings")]
    [SerializeField] private bool enableTrails = true;
    [SerializeField] private float trailSpacing = 1f;
    [SerializeField] private float footprintSize = 2f; // Size in world units

    [Header("Performance")]
    [SerializeField] [Tooltip("Decay every N frames (1=smoothest, 2-3=recommended, 5+=fastest)")]
    private int decayUpdateRate = 3; // Slightly less frequent to handle 2048 texture
    
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
        if (debugCanvas != null) Destroy(debugCanvas.gameObject);
    }

    private void InitializeRenderTextures()
    {
        Debug.Log("[GrassTrampleSystem] Initializing RenderTextures...");
        
        // Use ARGB32 instead of ARGBFloat for better compatibility
        // Main trample map (R = strength, GB = direction for future use)
        trampleMap = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
        trampleMap.filterMode = filterMode;
        trampleMap.wrapMode = TextureWrapMode.Clamp;
        trampleMap.Create();
        Debug.Log($"[GrassTrampleSystem] Created trampleMap: {textureResolution}x{textureResolution}");

        // Temp map for decay operations
        tempMap = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
        tempMap.filterMode = filterMode;
        tempMap.wrapMode = TextureWrapMode.Clamp;
        tempMap.Create();

        // Clear to black using CommandBuffer
        commandBuffer = new CommandBuffer();
        commandBuffer.name = "TrampleSystem";
        commandBuffer.SetRenderTarget(trampleMap);
        commandBuffer.ClearRenderTarget(false, true, Color.clear); // Don't clear depth
        Graphics.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();

        // Create materials
        Shader decayShader = Shader.Find("Hidden/GrassTrampleDecay");
        Shader stampShader = Shader.Find("Hidden/GrassTrampleStamp");
        
        if (decayShader == null) 
            Debug.LogError("[GrassTrampleSystem] Decay shader not found! Make sure 'Hidden/GrassTrampDecay' exists.");
        else
            Debug.Log("[GrassTrampleSystem] Found decay shader");
            
        if (stampShader == null) 
            Debug.LogError("[GrassTrampleSystem] Stamp shader not found! Make sure 'Hidden/GrassTrampStamp' exists.");
        else
            Debug.Log("[GrassTrampleSystem] Found stamp shader");
        
        decayMaterial = new Material(decayShader);
        stampMaterial = new Material(stampShader);

        // Set global shader properties
        Shader.SetGlobalTexture("_TrampleMap", trampleMap);
        Shader.SetGlobalVector("_GridWorldMin", new Vector4(terrainWorldMin.x, 0, terrainWorldMin.y, 0));
        Shader.SetGlobalVector("_GridWorldSize", new Vector4(terrainWorldSize.x, 0, terrainWorldSize.y, 0));
        
        Debug.Log($"[GrassTrampleSystem] Terrain bounds: Min({terrainWorldMin.x}, {terrainWorldMin.y}) Size({terrainWorldSize.x}, {terrainWorldSize.y})");
        
        // Create debug UI
        CreateDebugUI();
        
        Debug.Log("[GrassTrampleSystem] Initialization complete!");
    }

    void Update()
    {
        if (!enableTrails) return;

        frameCounter++;
        
        // Only decay every N frames to reduce overhead
        if (frameCounter % decayUpdateRate == 0)
        {
            ApplyDecay();
        }
        
        UpdateTramplers();
        
        // Update debug UI less frequently
        if (frameCounter % 30 == 0)
        {
            UpdateDebugUI();
        }
        
        // Update global time for wind
        Shader.SetGlobalFloat("_GlobalTime", Time.time);
    }

    private void ApplyDecay()
    {
        // Fade the trample map over time using CommandBuffer
        // Adjust decay based on frame rate to compensate for skipped frames
        float decayFactor = 1f - (decayRate * Time.deltaTime * decayUpdateRate);
        decayMaterial.SetFloat("_DecayFactor", Mathf.Clamp01(decayFactor));
        
        // commandBuffer.Clear();
        // commandBuffer.Blit(trampleMap, tempMap, decayMaterial);
        // commandBuffer.Blit(tempMap, trampleMap);
        // Graphics.ExecuteCommandBuffer(commandBuffer);
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
        // Convert world position to UV coordinates (0-1)
        Vector2 uv = WorldToUV(new Vector2(worldPosition.x, worldPosition.z));
                
        // Check if UV is out of bounds
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
        {
            Debug.LogWarning($"[GrassTrampleSystem] Stamp UV out of bounds! Position might be outside terrain bounds. Check Terrain World Min/Size settings.");
        }
        
        // Convert direction to texture space (-1 to 1, stored as 0-1)
        Vector2 dirNormalized = new Vector2(direction.x, direction.z).normalized;
        Vector2 dirEncoded = dirNormalized * 0.5f + Vector2.one * 0.5f; // 0-1 range

        stampMaterial.SetVector("_StampCenter", new Vector4(uv.x, uv.y, 0, 0));
        stampMaterial.SetFloat("_StampRadius", footprintSize / terrainWorldSize.x); // Normalized radius
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
    }

    public static void UnregisterTrampler(Transform trampler)
    {
        if (instance == null) return;
        instance.activeTramplers.RemoveAll(t => t.transform == trampler);
    }

    // UI Debug Visualization
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugView = true;
    [SerializeField] private int debugViewSize = 256;
    
    private Canvas debugCanvas;
    private UnityEngine.UI.RawImage debugImage;
    private UnityEngine.UI.Text debugText;
    
    private void CreateDebugUI()
    {
        if (!showDebugView) return;
        
        // Create Canvas
        GameObject canvasObj = new GameObject("TrampleDebugCanvas");
        canvasObj.transform.SetParent(transform);
        debugCanvas = canvasObj.AddComponent<Canvas>();
        debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        debugCanvas.sortingOrder = 9999;
        
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Create background panel
        GameObject panelObj = new GameObject("DebugPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image panel = panelObj.AddComponent<UnityEngine.UI.Image>();
        panel.color = new Color(0, 0, 0, 0.7f);
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0); // Bottom-left anchor
        panelRect.anchorMax = new Vector2(0, 0); // Bottom-left anchor
        panelRect.pivot = new Vector2(0, 0);     // Bottom-left pivot
        panelRect.anchoredPosition = new Vector2(10, 10); // 10 pixels from bottom-left
        panelRect.sizeDelta = new Vector2(debugViewSize + 20, debugViewSize + 60);
        
        // Create title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform, false);
        debugText = titleObj.AddComponent<UnityEngine.UI.Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugText.text = "Trample Map";
        debugText.fontSize = 14;
        debugText.color = Color.white;
        debugText.alignment = TextAnchor.LowerCenter;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -5);
        titleRect.sizeDelta = new Vector2(0, 30);
        
        // Create RawImage for trample map
        GameObject imageObj = new GameObject("TrampleImage");
        imageObj.transform.SetParent(panelObj.transform, false);
        debugImage = imageObj.AddComponent<UnityEngine.UI.RawImage>();
        debugImage.texture = trampleMap;
        
        RectTransform imageRect = imageObj.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 1);
        imageRect.anchorMax = new Vector2(0.5f, 1);
        imageRect.pivot = new Vector2(0.5f, 1);
        imageRect.anchoredPosition = new Vector2(0, -40);
        imageRect.sizeDelta = new Vector2(debugViewSize, debugViewSize);
        
        // Create stats text
        GameObject statsObj = new GameObject("StatsText");
        statsObj.transform.SetParent(panelObj.transform, false);
        UnityEngine.UI.Text statsText = statsObj.AddComponent<UnityEngine.UI.Text>();
        statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statsText.text = "Active: 0";
        statsText.fontSize = 12;
        statsText.color = Color.white;
        statsText.alignment = TextAnchor.UpperLeft;
        
        RectTransform statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 0);
        statsRect.anchorMax = new Vector2(1, 0);
        statsRect.pivot = new Vector2(0, 0);
        statsRect.anchoredPosition = new Vector2(10, 5);
        statsRect.sizeDelta = new Vector2(-20, 20);
        
        Debug.Log("[GrassTrampleSystem] Debug UI created!");
    }
    
    private void UpdateDebugUI()
    {
        if (!showDebugView || debugCanvas == null || debugText == null) return;
        
        // Simple text update - no texture readback
        debugText.text = $"Trample Map - Active: {activeTramplers.Count}";
    }
}
