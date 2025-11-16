using UnityEngine;

// Attach this to any GameObject to debug the trample system
public class TrampleDebugTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool testManualStamp = false;
    [SerializeField] private Vector3 testStampPosition = Vector3.zero;

    public PlayerInputs inputs;
    
    void Update()
    {
        // Press T to stamp at test position
        if (inputs.debug2 || testManualStamp)
        {
            inputs.debug2 = false;
            testManualStamp = false;
            TestStamp();
        }
        
        // Check shader globals
        if (inputs.debug1)
        {
            inputs.debug1 = false;
            CheckShaderGlobals();
        }
    }
    
    private void TestStamp()
    {
        Debug.Log($"[TrampleTest] Attempting manual stamp at {testStampPosition}");
        
        // This won't work directly, but we can check if the system exists
        var system = FindAnyObjectByType<GrassTrampleSystem>();
        if (system == null)
        {
            Debug.LogError("[TrampleTest] No GrassTrampleSystem found in scene!");
            return;
        }
        
        Debug.Log("[TrampleTest] GrassTrampleSystem found. Walk around to create footprints.");
    }
    
    private void CheckShaderGlobals()
    {
        Debug.Log("=== Shader Global Properties ===");
        
        Texture trampleMap = Shader.GetGlobalTexture("_TrampleMap");
        if (trampleMap != null)
        {
            Debug.Log($"[TrampleTest] _TrampleMap: Found ({trampleMap.width}x{trampleMap.height})");
            
            // Check if it's actually a RenderTexture
            RenderTexture rt = trampleMap as RenderTexture;
            if (rt != null)
            {
                Debug.Log($"[TrampleTest] Is RenderTexture: YES, Format: {rt.format}");
            }
        }
        else
        {
            Debug.LogError("[TrampleTest] _TrampleMap: NOT FOUND!");
        }
        
        Vector4 gridMin = Shader.GetGlobalVector("_GridWorldMin");
        Vector4 gridSize = Shader.GetGlobalVector("_GridWorldSize");
        float globalTime = Shader.GetGlobalFloat("_GlobalTime");
        
        Debug.Log($"[TrampleTest] _GridWorldMin: {gridMin}");
        Debug.Log($"[TrampleTest] _GridWorldSize: {gridSize}");
        Debug.Log($"[TrampleTest] _GlobalTime: {globalTime}");
        
        // Calculate what UV your current position would be
        Vector3 playerPos = transform.position;
        float uvX = (playerPos.x - gridMin.x) / gridSize.x;
        float uvY = (playerPos.z - gridMin.y) / gridSize.y;
        Debug.Log($"[TrampleTest] Your position {playerPos} = UV ({uvX:F3}, {uvY:F3})");
        
        if (uvX < 0 || uvX > 1 || uvY < 0 || uvY > 1)
        {
            Debug.LogWarning("[TrampleTest] YOU ARE OUTSIDE THE TERRAIN BOUNDS! Adjust Terrain World Min/Size.");
        }
        
        // Check if grass materials are using the right shader
        var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int grassCount = 0;
        foreach (var renderer in renderers)
        {
            if (renderer.sharedMaterial != null && 
                renderer.sharedMaterial.shader.name.Contains("GrassTrample"))
            {
                grassCount++;
                Debug.Log($"[TrampleTest] Found grass material: {renderer.gameObject.name} - Shader: {renderer.sharedMaterial.shader.name}");
                
                // Check shader compilation
                if (renderer.sharedMaterial.shader.isSupported)
                {
                    Debug.Log($"[TrampleTest] - Shader IS SUPPORTED ✓");
                }
                else
                {
                    Debug.LogError($"[TrampleTest] - Shader NOT SUPPORTED! Check for compilation errors.");
                }
            }
        }
        
        if (grassCount == 0)
        {
            Debug.LogWarning("[TrampleTest] No materials using GrassTrample shader found! Make sure your grass uses 'Custom/GrassTrampleRenderTexture_ShadowReceiving'");
        }
        else
        {
            Debug.Log($"[TrampleTest] Found {grassCount} grass materials with trample shader");
        }
        
        Debug.Log("=== Summary ===");
        Debug.Log($"Global texture: {(trampleMap != null ? "SET ✓" : "MISSING ✗")}");
        Debug.Log($"Terrain bounds: Min({gridMin.x}, {gridMin.y}) Size({gridSize.x}, {gridSize.y})");
        Debug.Log($"Your UV: ({uvX:F3}, {uvY:F3}) {(uvX >= 0 && uvX <= 1 && uvY >= 0 && uvY <= 1 ? "VALID ✓" : "OUT OF BOUNDS ✗")}");
        Debug.Log($"Grass materials: {grassCount}");
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, Screen.height - 100, 300, 90));
        GUILayout.Label("=== Trample Debug ===");
        GUILayout.Label("Press T: Test stamp");
        GUILayout.Label("Press G: Check shader globals");
        GUILayout.EndArea();
    }
}
