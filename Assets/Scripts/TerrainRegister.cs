using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainRegister : MonoBehaviour
{
    private void Awake()
    {
        var terrain = GetComponent<Terrain>();
        GlobalVariables.RegisterTerrain(terrain);
    }
}