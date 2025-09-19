using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager Instance { get; private set; }

    private List<Terrain> terrains = new List<Terrain>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Find all terrains in the scene
        terrains.AddRange(FindObjectsByType<Terrain>(FindObjectsSortMode.None));
    }

    // Returns the terrain height at a given world position.
    public float GetTerrainHeight(Vector3 worldPos)
    {
        foreach (var terrain in terrains)
        {
            if (terrain == null) continue;

            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;

            // Check if worldPos is within this terrain bounds
            if (worldPos.x >= tPos.x && worldPos.x <= tPos.x + tSize.x &&
                worldPos.z >= tPos.z && worldPos.z <= tPos.z + tSize.z)
            {
                return terrain.SampleHeight(worldPos) + tPos.y;
            }
        }

        // Default if no terrain found
        return 0f;
    }

    // Get the terrain under a world position
    public Terrain GetTerrainAtPosition(Vector3 worldPos)
    {
        foreach (var terrain in terrains)
        {
            if (terrain == null) continue;

            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;

            if (worldPos.x >= tPos.x && worldPos.x <= tPos.x + tSize.x &&
                worldPos.z >= tPos.z && worldPos.z <= tPos.z + tSize.z)
            {
                return terrain;
            }
        }
        return null;
    }
}
