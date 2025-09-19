using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class HerdManager : MonoBehaviour
{
    public static HerdManager Instance { get; private set; }

    [Header("Herd Settings")]
    public GameObject herdPrefab;
    public int maxHerds = 10;
    public float herdSeparation = 200f; // minimum distance between herds
    public float spawnAreaRadius = 1000f; // world radius (centered at origin, or set dynamically)
    private bool initialized = false;

    private readonly List<Herd> activeHerds = new List<Herd>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!initialized)
        {
            InitializeHerds();
        }
    }

    public void InitializeHerds()
    {
        // Initial population
        // for (int i = 0; i < maxHerds; i++)
        // {
        //     TrySpawnHerd();
        // }
        Debug.Log("Active herds: " + activeHerds);

        initialized = true;
    }

    public void TrySpawnHerd()
    {
        if (activeHerds.Count >= maxHerds)
            return;

        Vector3? pos = GetValidSpawnPosition();
        Debug.Log(pos);
        if (pos.HasValue)
        {
            GameObject herdObj = Instantiate(herdPrefab, pos.Value, Quaternion.identity);
            Herd herd = herdObj.GetComponent<Herd>();
            if (herd != null)
            {
                activeHerds.Add(herd);
                herd.transform.position = pos.Value; // set position
            }
        }
    }

    private Vector3? GetValidSpawnPosition()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            // Pick a random point within rectangular bounds from GlobalVariables
            float x = Random.Range(GlobalVariables.mapMin.x, GlobalVariables.mapMax.x);
            float z = Random.Range(GlobalVariables.mapMin.z, GlobalVariables.mapMax.z);
            Vector3 candidate = new Vector3(x, 0, z);

            // Check NavMesh validity
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 20f, NavMesh.AllAreas))
                continue;

            // Check separation from other herds using transform.position
            bool tooClose = false;
            foreach (var herd in activeHerds)
            {
                if (Vector3.Distance(hit.position, herd.transform.position) < herdSeparation)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            return hit.position; // valid spot found
        }

        return null; // no valid position found
    }

    public void RegisterHerd(Herd herd)
    {
        if (!activeHerds.Contains(herd))
            activeHerds.Add(herd);
    }

    public void UnregisterHerd(Herd herd)
    {
        if (activeHerds.Contains(herd))
            activeHerds.Remove(herd);
    }
}
