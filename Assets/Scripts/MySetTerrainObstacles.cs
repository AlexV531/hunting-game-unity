using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MySetTerrainObstacles : MonoBehaviour
{
    public void GenerateObstacles()
    {
        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        int count = 0;

        foreach (Terrain terrain in terrains)
        {
            TreeInstance[] Obstacle;

            if (terrain == null)
            {
                Debug.LogError("No active terrain found.");
                return;
            }

            Obstacle = terrain.terrainData.treeInstances;

            float width = terrain.terrainData.size.x;
            float length = terrain.terrainData.size.z;
            float height = terrain.terrainData.size.y;

            Debug.Log("Terrain Size is: " + width + " , " + height + " , " + length);
            Debug.Log("This is Mine");

            GameObject parent = new GameObject("Tree_Obstacles_" + count);

            bool isError = false;
            Debug.Log("Adding " + Obstacle.Length + " NavMeshObstacle Components for Trees");

            for (int i = 0; i < Obstacle.Length; i++)
            {
                TreeInstance tree = Obstacle[i];
                Vector3 tempPos = new Vector3(tree.position.x * width + terrain.GetPosition().x, tree.position.y * height + terrain.GetPosition().y, tree.position.z * length + terrain.GetPosition().z);
                Quaternion tempRot = Quaternion.AngleAxis(tree.rotation * Mathf.Rad2Deg, Vector3.up);

                GameObject obs = new GameObject("Obstacle" + i);
                obs.transform.SetParent(parent.transform);
                obs.transform.position = tempPos;
                obs.transform.rotation = tempRot;

                NavMeshObstacle obsElement = obs.AddComponent<NavMeshObstacle>();
                obsElement.carving = true;
                obsElement.carveOnlyStationary = true;

                Collider coll = terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab.GetComponent<Collider>();
                if (coll == null || (coll.GetType() != typeof(CapsuleCollider) && coll.GetType() != typeof(BoxCollider)))
                {
                    isError = true;
                    Debug.LogError("ERROR: Tree prefab '" + terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab.name + "' has no BoxCollider or CapsuleCollider.");
                    break;
                }

                if (coll is CapsuleCollider capsuleColl)
                {
                    obsElement.shape = NavMeshObstacleShape.Capsule;
                    obsElement.center = capsuleColl.center;
                    obsElement.radius = capsuleColl.radius;
                    obsElement.height = capsuleColl.height;
                }
                else if (coll is BoxCollider boxColl)
                {
                    obsElement.shape = NavMeshObstacleShape.Box;
                    obsElement.center = boxColl.center;
                    obsElement.size = boxColl.size;
                }

                parent.transform.position = terrain.GetPosition();
            }

            count++;

            if (!isError) Debug.Log("All " + Obstacle.Length + " NavMeshObstacles were successfully added to your Scene!");
        }
    }
}