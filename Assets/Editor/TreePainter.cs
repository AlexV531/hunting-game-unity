using UnityEngine;
using UnityEditor;

public class TreePainter : EditorWindow
{
    public Terrain terrain;
    public GameObject treePrefab;
    public float radius = 10f;
    public float minDistance = 5f;

    [MenuItem("Tools/Tree Painter")]
    public static void Open() => GetWindow<TreePainter>();

    void OnGUI()
    {
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        treePrefab = (GameObject)EditorGUILayout.ObjectField("Tree Prefab", treePrefab, typeof(GameObject), false);
        radius = EditorGUILayout.FloatField("Radius", radius);
        minDistance = EditorGUILayout.FloatField("Min Distance", minDistance);
    }

    void OnSceneGUI()
    {
        if (terrain == null || treePrefab == null) return;
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.GetComponent<Terrain>() == terrain)
                {
                    Vector3 pos = hit.point;
                    var trees = new System.Collections.Generic.List<TreeInstance>(terrain.terrainData.treeInstances);
                    bool tooClose = false;

                    foreach (var t in trees)
                    {
                        Vector3 worldPos = Vector3.Scale(t.position, terrain.terrainData.size) + terrain.transform.position;
                        if (Vector3.Distance(worldPos, pos) < minDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        TreeInstance tree = new TreeInstance();
                        tree.position = new Vector3(pos.x / terrain.terrainData.size.x, pos.y / terrain.terrainData.size.y, pos.z / terrain.terrainData.size.z);
                        tree.prototypeIndex = 0;
                        tree.widthScale = 1f;
                        tree.heightScale = 1f;
                        tree.color = Color.white;
                        tree.lightmapColor = Color.white;
                        trees.Add(tree);
                        terrain.terrainData.treeInstances = trees.ToArray();
                    }

                    e.Use();
                }
            }
        }
    }
}
