using UnityEngine;

public class MapObject : MonoBehaviour
{
    public static MapObject Instance { get; private set; }
    
    void Awake()
    {
        Instance = this;
    }
}