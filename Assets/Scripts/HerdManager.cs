using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class HerdManager : MonoBehaviour
{
    public static HerdManager Instance { get; private set; }

    [Header("Herd Settings")]
    public GameObject herdPrefab;
    public GameObject[] animalPrefabs;
    public int[] maxHerdForAnimal;
    public int[] maxAnimalInHerdForAnimal;
    public float herdSeparation = 100f;

    [Header("Need Zone Settings")]
    public float needThreshold = 30f; // Start seeking need zone when below this
    public float needSatisfiedThreshold = 80f; // Leave zone when above this
    public float stayAtZoneMinTime = 180f; // Minimum time to stay at zone
    public float needDrainedPerSecond = 0.01f;
    public float needRestoredPerSecond = 0.1f;

    private bool initialized = false;
    private int maxAttempts = 30;
    private readonly List<Herd> activeHerds = new List<Herd>();
    private readonly List<NeedZone> needZones = new List<NeedZone>();

    // Track which herds are traveling to which zones
    private readonly Dictionary<Herd, NeedZone> herdDestinations = new Dictionary<Herd, NeedZone>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Find all need zones in the scene
        needZones.AddRange(FindObjectsByType<NeedZone>(FindObjectsSortMode.None));
        Debug.Log($"Found {needZones.Count} need zones");
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!initialized)
        {
            InitializeHerds();
        }
        else
        {
            UpdateHerdNeeds();
        }
    }

    public void InitializeHerds()
    {
        if (!(maxAnimalInHerdForAnimal.Length == maxHerdForAnimal.Length &&
              maxHerdForAnimal.Length == animalPrefabs.Length))
        {
            Debug.LogError("Herd manager data arrays not equal lengths.");
            return;
        }

        for (int j = 0; j < animalPrefabs.Length; j++)
        {
            for (int i = 0; i < maxHerdForAnimal[j]; i++)
            {
                TrySpawnHerd(animalPrefabs[j], maxHerdForAnimal[j], maxAnimalInHerdForAnimal[j]);
            }
        }

        initialized = true;
    }

    public void TrySpawnHerd(GameObject animalPrefab, int maxHerds, int maxAnimals)
    {
        Vector3? pos = GetValidSpawnPosition();

        if (pos.HasValue)
        {
            GameObject herdObj = Instantiate(herdPrefab, pos.Value, Quaternion.identity);
            Herd herd = herdObj.GetComponent<Herd>();

            if (herd != null)
            {
                activeHerds.Add(herd);
                herd.transform.position = pos.Value;
                herd.InitializeAnimals(maxAnimals, animalPrefab);

                // Initialize needs component
                HerdNeedsComponent needsComp = herd.gameObject.AddComponent<HerdNeedsComponent>();
                needsComp.needs = new HerdNeeds();
            }
        }
    }

    private void UpdateHerdNeeds()
    {
        foreach (var herd in activeHerds)
        {
            // if (!herd.IsActive())
            //     continue;

            if (herd.IsHerdFleeing())
                continue;

            var needsComp = herd.GetComponent<HerdNeedsComponent>();
            if (needsComp == null)
                continue;

            var needs = needsComp.needs;

            // Check if herd is at a need zone
            NeedZone currentZone = GetNeedZoneAtPosition(herd.transform.position);

            if (currentZone != null && currentZone.occupyingHerd == herd)
            {
                if (herd.AreAllAnimalsInRadius())
                {
                    // Refill the need
                    needs.RefillNeed(currentZone.needType, Time.deltaTime * needRestoredPerSecond);
                    needsComp.timeAtCurrentZone += Time.deltaTime;

                    // Check if need is satisfied and minimum time has passed
                    if (needs.GetNeedValue(currentZone.needType) >= needSatisfiedThreshold &&
                        needsComp.timeAtCurrentZone >= stayAtZoneMinTime)
                    {
                        // Leave the zone
                        currentZone.Release();
                        herdDestinations.Remove(herd);
                        needsComp.timeAtCurrentZone = 0f;
                        Debug.Log($"Herd satisfied {currentZone.needType} need, leaving zone");

                        Vector3 wanderTarget = herd.GetRandomMovementPoint(300f); // wander within 300 meters of current position
                        List<Vector3> wanderList = new List<Vector3> { wanderTarget };
                        herd.HerdMoveTo(wanderList);
                    }
                }
            }
            else
            {
                // Drain needs over time
                needs.DrainNeeds(Time.deltaTime * needDrainedPerSecond);

                // Check if herd needs to go to a zone
                NeedType lowestNeed = needs.GetLowestNeed();
                float lowestValue = needs.GetNeedValue(lowestNeed);

                if (lowestValue < needThreshold && !herdDestinations.ContainsKey(herd))
                {
                    // Find nearest available zone
                    NeedZone targetZone = FindNearestAvailableZone(herd.transform.position, lowestNeed);

                    if (targetZone != null)
                    {
                        // Reserve the zone
                        targetZone.TryOccupy(herd);
                        herdDestinations[herd] = targetZone;

                        // Move herd to zone
                        List<Vector3> zoneList = new List<Vector3> { targetZone.transform.position };
                        herd.HerdMoveTo(zoneList);

                        Debug.Log($"Herd heading to {lowestNeed} zone at {targetZone.transform.position}");
                    }
                }
            }
        }
    }

    private NeedZone GetNeedZoneAtPosition(Vector3 position)
    {
        foreach (var zone in needZones)
        {
            if (Vector3.Distance(position, zone.transform.position) <= zone.radius)
            {
                return zone;
            }
        }
        return null;
    }

    private NeedZone FindNearestAvailableZone(Vector3 position, NeedType needType)
    {
        var availableZones = needZones
            .Where(z => z.needType == needType && !z.isOccupied)
            .OrderBy(z => Vector3.Distance(position, z.transform.position))
            .ToList();

        return availableZones.FirstOrDefault();
    }

    private Vector3? GetValidSpawnPosition()
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float x = Random.Range(GlobalVariables.mapMin.x, GlobalVariables.mapMax.x);
            float z = Random.Range(GlobalVariables.mapMin.z, GlobalVariables.mapMax.z);
            Vector3 candidate = new Vector3(x, 0, z);

            if (!UnityEngine.AI.NavMesh.SamplePosition(candidate, out UnityEngine.AI.NavMeshHit hit, 20f, UnityEngine.AI.NavMesh.AllAreas))
                continue;

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
            return hit.position;
        }
        return null;
    }

    public void RegisterHerd(Herd herd)
    {
        if (!activeHerds.Contains(herd))
            activeHerds.Add(herd);
    }

    public void UnregisterHerd(Herd herd)
    {
        if (activeHerds.Contains(herd))
        {
            activeHerds.Remove(herd);

            // Clean up any zone reservations
            if (herdDestinations.TryGetValue(herd, out NeedZone zone))
            {
                zone.Release();
                herdDestinations.Remove(herd);
            }
        }
    }
}
