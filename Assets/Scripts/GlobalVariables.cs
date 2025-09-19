using UnityEngine;

public class GlobalVariables : MonoBehaviour
{
    public static Vector3 debugTarget = Vector3.zero;

    // public static Terrain terrain { get; private set; }
    public static Vector3 mapMin = new Vector3(0f, -100f, 0f);
    public static Vector3 mapMax = new Vector3(2000f, 500f, 2000f);

    // public static PlayerInputs playerInputs { get; private set; }

    public static float cameraFOV = 60f;

    // public static float GetTerrainHeightAtWorldPos(Vector3 worldPos)
    // {
    //     // Convert world pos → terrain local pos (0–1)
    //     float relativeX = (worldPos.x - terrain.transform.position.x) / terrain.terrainData.size.x;
    //     float relativeZ = (worldPos.z - terrain.transform.position.z) / terrain.terrainData.size.z;

    //     // Convert → heightmap pixel coords
    //     int x = Mathf.RoundToInt(relativeX * terrain.terrainData.heightmapResolution);
    //     int z = Mathf.RoundToInt(relativeZ * terrain.terrainData.heightmapResolution);

    //     x = Mathf.Clamp(x, 0, terrain.terrainData.heightmapResolution - 1);
    //     z = Mathf.Clamp(z, 0, terrain.terrainData.heightmapResolution - 1);

    //     return terrain.terrainData.GetHeight(x, z) + terrain.transform.position.y;
    // }

    // public static void RegisterTerrain(Terrain t)
    // {
    //     terrain = t;
    // }

    // public static void RegisterPlayerInputs(PlayerInputs inputs)
    // {
    //     playerInputs = inputs;
    // }
}
