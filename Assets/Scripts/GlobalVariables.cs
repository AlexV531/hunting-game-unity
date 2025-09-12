using UnityEngine;

public class GlobalVariables : MonoBehaviour
{
    public static Vector3 debugTarget = Vector3.zero;

    public static Vector3 mapMin = new Vector3(0f, -100f, 0f);
    public static Vector3 mapMax = new Vector3(1000f, 500f, 1000f);

    public static float cameraFOV = 60f;

    public static float GetTerrainHeightAtWorldPos(Vector3 worldPos, TerrainData tData, Vector3 terrainPos)
    {
        // Convert world pos → terrain local pos (0–1)
        float relativeX = (worldPos.x - terrainPos.x) / tData.size.x;
        float relativeZ = (worldPos.z - terrainPos.z) / tData.size.z;

        // Convert → heightmap pixel coords
        int x = Mathf.RoundToInt(relativeX * tData.heightmapResolution);
        int z = Mathf.RoundToInt(relativeZ * tData.heightmapResolution);

        x = Mathf.Clamp(x, 0, tData.heightmapResolution - 1);
        z = Mathf.Clamp(z, 0, tData.heightmapResolution - 1);

        return tData.GetHeight(x, z) + terrainPos.y;
    }
}
