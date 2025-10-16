using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class HerdManager : MonoBehaviour
{
    public static HerdManager Instance { get; private set; }

    [Header("Herd Settings")]
    public GameObject herdPrefab;
    public GameObject[] animalPrefabs;
    public int[] maxHerdForAnimal;
    public int[] maxAnimalInHerdForAnimal;
    public float herdSeparation = 100f; // minimum distance between herds
    private bool initialized = false;
    private int maxAttempts = 30;

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
        if (!(maxAnimalInHerdForAnimal.Length == maxHerdForAnimal.Length && maxHerdForAnimal.Length == animalPrefabs.Length))
        {
            Debug.LogError("Herd manager data arrays not equal lengths.");
        }
        for (int j = 0; j < animalPrefabs.Length; j++)
        {
            // Initial population
            for (int i = 0; i < maxHerdForAnimal[j]; i++)
            {
                TrySpawnHerd(animalPrefabs[j], maxHerdForAnimal[j], maxAnimalInHerdForAnimal[j]);
            }
        }

        initialized = true;
    }

    public void TrySpawnHerd(GameObject animalPrefab, int maxHerds, int maxAnimals)
    {
        // if (activeHerds.Count >= maxHerds)
        //     return;

        Vector3? pos = GetValidSpawnPosition();
        Debug.Log("Trying to spawn a herd of " + animalPrefab.name + " at position " + pos);
        if (pos.HasValue)
        {
            GameObject herdObj = Instantiate(herdPrefab, pos.Value, Quaternion.identity);
            Herd herd = herdObj.GetComponent<Herd>();
            if (herd != null)
            {
                activeHerds.Add(herd);
                herd.transform.position = pos.Value; // set position
                herd.InitializeAnimals(maxAnimals, animalPrefab);
            }
        }
    }

    private Vector3? GetValidSpawnPosition()
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Pick a random point within rectangular bounds from GlobalVariables
            float x = Random.Range(GlobalVariables.mapMin.x, GlobalVariables.mapMax.x);
            float z = Random.Range(GlobalVariables.mapMin.z, GlobalVariables.mapMax.z);
            // float x = Random.Range(0, 100);
            // float z = Random.Range(0, 100);
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
