using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class MapMenu : UIMenu
{
    [Header("Map References")]
    public GameObject mapObject;
    public Transform cameraRoot;
    
    [Header("Map Position")]
    public float mapDistance = 2f;
    
    [Header("Camera Angle")]
    [Range(-90f, 90f)]
    public float cameraLookAngle = 0f; // 0 = straight forward, positive = look down, negative = look up
    
    [Header("Pan Settings")]
    public float panSpeed = 0.5f;
    public Vector2 panBoundsMin = new Vector2(-10f, -10f);
    public Vector2 panBoundsMax = new Vector2(10f, 10f);
    
    [Header("Scroll/Zoom Settings")]
    public float scrollSpeed = 0.2f;
    public float minZoomDistance = 1f;
    public float maxZoomDistance = 5f;
    
    private Vector3 mapPanOffset = Vector3.zero;
    private Vector2 lastMousePosition;
    private Quaternion originalCameraRotation;
    
    protected override void Start()
    {
        base.Start();
        
        // Find the map object if not assigned
        if (mapObject == null && MapObject.Instance != null)
        {
            mapObject = MapObject.Instance.gameObject;
        }
        
        if (mapObject != null)
        {
            mapObject.SetActive(false);
        }
    }
    
    void Update()
    {
        // Handle map panning when open
        if (IsMenuOpen())
        {
            HandleMapPanning();
        }
    }
    
    void ToggleMap()
    {
        if (IsMenuOpen())
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }
    
    public override void OpenMenu()
    {
        base.OpenMenu();
        
        if (cameraRoot != null)
        {
            originalCameraRotation = cameraRoot.rotation;
            // Keep Y rotation (compass direction), set X to configurable angle, zero out Z
            cameraRoot.rotation = Quaternion.Euler(cameraLookAngle, cameraRoot.eulerAngles.y, 0f);
        }
        
        if (mapObject != null && cameraRoot != null)
        {
            mapObject.transform.SetParent(cameraRoot);
            
            // Position map in front of camera
            mapObject.transform.localPosition = new Vector3(0, 0, mapDistance);
            mapObject.transform.localRotation = Quaternion.identity;
            
            mapPanOffset = Vector3.zero;
            mapObject.SetActive(true);
        }
    }
    
    public override void CloseMenu()
    {
        base.CloseMenu();
        
        if (mapObject != null)
        {
            mapObject.SetActive(false);
        }
        
        if (cameraRoot != null)
        {
            cameraRoot.rotation = originalCameraRotation;
        }
    }
    
    void HandleMapPanning()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        
        // Left mouse button to pan
        if (mouse.leftButton.wasPressedThisFrame)
        {
            lastMousePosition = mouse.position.ReadValue();
        }
        
        if (mouse.leftButton.isPressed)
        {
            Vector2 currentMousePosition = mouse.position.ReadValue();
            Vector2 mouseDelta = currentMousePosition - lastMousePosition;
            
            // Pan in X and Y (left/right and up/down on screen)
            Vector3 panDelta = new Vector3(mouseDelta.x, mouseDelta.y, 0) * panSpeed * Time.deltaTime;
            
            // Apply panning with limits
            mapPanOffset += panDelta;
            mapPanOffset.x = Mathf.Clamp(mapPanOffset.x, panBoundsMin.x, panBoundsMax.x);
            mapPanOffset.y = Mathf.Clamp(mapPanOffset.y, panBoundsMin.y, panBoundsMax.y);
            
            // Update map position
            if (mapObject != null)
            {
                mapObject.transform.localPosition = new Vector3(mapPanOffset.x, mapPanOffset.y, mapDistance);
            }
            
            lastMousePosition = currentMousePosition;
        }
        
        // Scroll wheel to zoom
        Vector2 scrollDelta = mouse.scroll.ReadValue();
        float scroll = scrollDelta.y / 120f;
        
        if (scroll != 0 && mapObject != null)
        {
            mapDistance = Mathf.Clamp(mapDistance - scroll * scrollSpeed, minZoomDistance, maxZoomDistance);
            Vector3 currentPos = mapObject.transform.localPosition;
            currentPos.z = mapDistance;
            mapObject.transform.localPosition = currentPos;
        }
    }
}
