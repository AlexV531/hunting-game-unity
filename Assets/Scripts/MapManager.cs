using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }
    
    [Header("Map Settings")]
    public RectTransform mapRect;
    public GameObject playerIconPrefab; // Icon for other players
    public GameObject localPlayerIconPrefab; // Icon for local player
    
    [Header("World Bounds")]
    public Vector2 worldMin = new Vector2(-50, -50);
    public Vector2 worldMax = new Vector2(50, 50);
    
    private Dictionary<GameObject, RectTransform> playerIcons = new Dictionary<GameObject, RectTransform>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    void Update()
    {
        UpdateAllIcons();
    }
    
    public void RegisterPlayer(GameObject player, bool isLocalPlayer = false)
    {
        if (player == null || playerIcons.ContainsKey(player)) return;
        
        // Choose which prefab to use
        GameObject prefabToUse = isLocalPlayer ? localPlayerIconPrefab : playerIconPrefab;
        
        GameObject iconObj = Instantiate(prefabToUse, mapRect);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        
        playerIcons.Add(player, iconRect);
    }
    
    public void UnregisterPlayer(GameObject player)
    {
        if (player == null) return;
        
        if (playerIcons.TryGetValue(player, out RectTransform icon))
        {
            Destroy(icon.gameObject);
            playerIcons.Remove(player);
        }
    }
    
    void UpdateAllIcons()
    {
        // Create a list of null keys to remove
        List<GameObject> nullKeys = new List<GameObject>();
        
        foreach (var kvp in playerIcons)
        {
            GameObject player = kvp.Key;
            RectTransform icon = kvp.Value;
            
            if (player == null)
            {
                // Mark for removal
                nullKeys.Add(player);
                if (icon != null)
                {
                    Destroy(icon.gameObject);
                }
                continue;
            }
            
            UpdateIconPositionAndRotation(player.transform, icon);
        }
        
        // Remove all null entries
        foreach (var nullKey in nullKeys)
        {
            playerIcons.Remove(nullKey);
        }
    }
    
    void UpdateIconPositionAndRotation(Transform trackedObject, RectTransform iconTransform)
    {
        float normalizedX = Mathf.InverseLerp(worldMin.x, worldMax.x, trackedObject.position.x);
        float normalizedY = Mathf.InverseLerp(worldMin.y, worldMax.y, trackedObject.position.z);
        
        float mapX = Mathf.Lerp(-mapRect.rect.width / 2, mapRect.rect.width / 2, normalizedX);
        float mapY = Mathf.Lerp(-mapRect.rect.height / 2, mapRect.rect.height / 2, normalizedY);
        
        iconTransform.anchoredPosition = new Vector2(mapX, mapY);
        
        // Track rotation - use the Y rotation (yaw) from the 3D object
        // This assumes your map is a top-down view
        float yRotation = trackedObject.eulerAngles.y;
        iconTransform.localRotation = Quaternion.Euler(0, 0, -yRotation);
    }
}
