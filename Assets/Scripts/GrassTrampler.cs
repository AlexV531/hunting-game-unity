using UnityEngine;

public class GrassTrampler : MonoBehaviour
{
    [Header("Trampler Settings")]
    [Tooltip("Offset from object position (useful if pivot isn't at feet)")]
    [SerializeField] private Vector3 trampleOffset = Vector3.zero;
    [SerializeField] private bool registerTramplerOnStart = false;
    
    [Tooltip("Visualize the trample radius in the scene view")]
    [SerializeField] private bool showDebugGizmo = true;
    
    // private Transform offsetTransform;
    
    void Start()
    {
        // // Create child object for offset if needed
        // if (trampleOffset != Vector3.zero)
        // {
        //     GameObject offsetObj = new GameObject("TramplePoint");
        //     offsetObj.transform.SetParent(transform);
        //     offsetObj.transform.localPosition = trampleOffset;
        //     offsetTransform = offsetObj.transform;
        // }
        // else
        // {
        //     offsetTransform = transform;
        // }
        
        if (registerTramplerOnStart)
            GrassTrampleSystem.RegisterTrampler(transform);
    }
    
    void OnDestroy()
    {
        GrassTrampleSystem.UnregisterTrampler(transform);
    }

    public void RegisterTrampler()
    {
        // GrassTrampleSystem.RegisterTrampler(transform);
    }

    public void UnregisterTrampler()
    {
        GrassTrampleSystem.UnregisterTrampler(transform);
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmo) return;
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        // Vector3 pos = Application.isPlaying && offsetTransform != null 
        //     ? offsetTransform.position 
        //     : transform.position + trampleOffset;
        Vector3 pos = transform.position;
        
        Gizmos.DrawSphere(pos, 1.5f); // Default radius visualization
    }
}