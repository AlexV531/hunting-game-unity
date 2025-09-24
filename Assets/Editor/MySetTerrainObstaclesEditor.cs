using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MySetTerrainObstacles))]
public class MySetTerrainObstaclesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw default fields if any

        if (GUILayout.Button("Generate Terrain Obstacles"))
        {
            MySetTerrainObstacles script = (MySetTerrainObstacles)target;
            script.GenerateObstacles();
        }
    }
}