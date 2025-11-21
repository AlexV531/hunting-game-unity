using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;

public class Herd : MonoBehaviour
{
    public readonly List<AnimalAI> animalsInHerd = new List<AnimalAI>();
    public float radius = 10f;
    public bool manuallyInitialize = false;
    public GameObject animalPrefab = null;

    [Header("Repopulation Settings")]
    public bool enableRepopulation = true;
    public int repopulationThreshold = 2; // Repopulate when at or below this number
    public int repopulationMinSize = 2;
    public int repopulationMaxSize = 4;
    private float activationDistance = 400f;
    private float deactivationOffset = 100f;
    private bool herdIsActive = false;

    void ManuallyInitialize()
    {
        if (animalPrefab != null)
        {
            InitializeAnimals(2, animalPrefab);
        }
    }

    void Update()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        
        if (manuallyInitialize)
        {
            Debug.Log("Initializing herd manually");
            manuallyInitialize = false;
            ManuallyInitialize();
        }

        HandleActivateDeactivate();
    }

    public void RegisterHerdAnimal(AnimalAI animalAI)
    {
        if (!animalsInHerd.Contains(animalAI))
        {
            animalAI.herd = this;
            animalsInHerd.Add(animalAI);
        }
    }

    public void UnregisterHerdAnimal(AnimalAI animalAI)
    {
        if (animalsInHerd.Contains(animalAI))
        {
            animalAI.herd = null;
            animalsInHerd.Remove(animalAI);
        }
    }

    public void InitializeAnimals(int maxNumAnimals, GameObject animalPrefab)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("InitializeAnimals should only be called on the server.");
            return;
        }

        // Store the prefab for future repopulation
        if (this.animalPrefab == null)
        {
            this.animalPrefab = animalPrefab;
        }

        // Determine the number of animals (larger herds are rarer)
        int numAnimals = 2;

        if (maxNumAnimals > 2)
        {
            // Bias toward smaller values: Random.value^2 or ^3 makes large numbers rarer
            float biased = Mathf.Pow(Random.value, 2.5f); // adjust exponent to control rarity (higher = rarer big herds)
            numAnimals = Mathf.RoundToInt(Mathf.Lerp(2, maxNumAnimals, biased));
        }

        Debug.Log("Herd initializing with " + numAnimals + " animals");

        // Spawn the animals
        for (int i = 0; i < numAnimals; i++)
        {
            SpawnAnimal(animalPrefab);
        }

        ActivateAnimals();
    }

    private void SpawnAnimal(GameObject animalPrefab)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        Vector3 spawnPos = GetRandomPointInRadius();

        GameObject animal = Instantiate(animalPrefab, spawnPos, Quaternion.identity);

        NetworkObject netObj = animal.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Animal prefab must have a NetworkObject component!");
            Destroy(animal);
            return;
        }

        // Spawn on the server to sync with clients
        netObj.Spawn();

        AnimalAI animalAI = animal.GetComponent<AnimalAI>();
        if (animalAI == null)
        {
            Debug.LogError("Animal prefab must have an AnimalAI component!");
            Destroy(animal);
            return;
        }

        Debug.Log("Spawning animal");

        RegisterHerdAnimal(animalAI);
    }

    public void TryRepopulate()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        
        if (!enableRepopulation || animalPrefab == null)
            return;
        
        if (animalsInHerd.Count <= repopulationThreshold)
        {
            int animalsToSpawn = Random.Range(repopulationMinSize, repopulationMaxSize + 1);
            
            for (int i = 0; i < animalsToSpawn; i++)
            {
                SpawnAnimal(animalPrefab);
            }
            
            Debug.Log($"Herd repopulated with {animalsToSpawn} animals. Total: {animalsInHerd.Count}");
        }
    }

    public void HerdFleeTo(List<Vector3> target_list, AnimalAI noticingAnimalAI)
    {
        transform.position = target_list[^1];
        for (int i = 0; i < animalsInHerd.Count; i++)
        {
            if (animalsInHerd[i] == noticingAnimalAI)
            {
                animalsInHerd[i].SetFleeing(GetRandomPointsInRadiusForArray(target_list));
            }
            else
            {
                StartCoroutine(animalsInHerd[i].DelayedSetFleeing(GetRandomPointsInRadiusForArray(target_list)));
            }
            
        }
    }

    public void HerdMoveTo(List<Vector3> target_list)
    {
        transform.position = target_list[^1];
        for (int i = 0; i < animalsInHerd.Count; i++)
        {
            StartCoroutine(animalsInHerd[i].DelayedSetMoving(GetRandomPointsInRadiusForArray(target_list)));
        }
    }

    public bool AreAllAnimalsInRadius()
    {
        if (animalsInHerd.Count == 0)
            return true;

        foreach (var animal in animalsInHerd)
        {
            if (animal == null)
                continue;

            Vector3 herdPos = transform.position;
            Vector3 animalPos = animal.transform.position;

            // Ignore Y-axis
            herdPos.y = 0f;
            animalPos.y = 0f;

            float dist = Vector3.Distance(herdPos, animalPos);
            if (dist > radius + 1f)
                return false;
        }

        return true;
    }

    public bool IsHerdPanicked()
    {
        if (animalsInHerd.Count == 0 || !IsActive())
        {
            return false;
        }

        foreach (var animal in animalsInHerd)
        {
            if (animal == null)
                continue;

            if (animal.IsPanicked())
            {
                return true;
            }
        }

        return false;
    }

    public Vector3 GetRandomPointInRadius()
    {
        // Random polar coordinates
        float r = radius * Mathf.Sqrt(Random.value);
        float theta = Random.value * Mathf.PI * 2f;

        Vector3 point = Vector3.zero;
        point.x = transform.position.x + r * Mathf.Cos(theta);
        point.z = transform.position.z + r * Mathf.Sin(theta);

        // Clamp within map bounds
        point.x = Mathf.Clamp(point.x, GlobalVariables.mapMin.x, GlobalVariables.mapMax.x);
        point.z = Mathf.Clamp(point.z, GlobalVariables.mapMin.z, GlobalVariables.mapMax.z);

        // Adjust Y to terrain height
        point.y = TerrainManager.Instance.GetTerrainHeight(point);

        return point;
    }

    public Vector3 GetRandomPointInRadius(float radius)
    {
        // Random polar coordinates
        float r = radius * Mathf.Sqrt(Random.value);
        float theta = Random.value * Mathf.PI * 2f;

        Vector3 point = Vector3.zero;
        point.x = transform.position.x + r * Mathf.Cos(theta);
        point.z = transform.position.z + r * Mathf.Sin(theta);

        // Clamp within map bounds
        point.x = Mathf.Clamp(point.x, GlobalVariables.mapMin.x, GlobalVariables.mapMax.x);
        point.z = Mathf.Clamp(point.z, GlobalVariables.mapMin.z, GlobalVariables.mapMax.z);

        // Adjust Y to terrain height
        point.y = TerrainManager.Instance.GetTerrainHeight(point);

        return point;
    }

    public List<Vector3> GetRandomPointsInRadiusForArray(List<Vector3> positions)
    {
        List<Vector3> newPositions = new List<Vector3>();

        foreach (Vector3 basePos in positions)
        {
            // Random polar coordinates
            float r = radius * Mathf.Sqrt(Random.value);
            float theta = Random.value * Mathf.PI * 2f;

            Vector3 point = Vector3.zero;
            point.x = basePos.x + r * Mathf.Cos(theta);
            point.z = basePos.z + r * Mathf.Sin(theta);

            // Clamp within map bounds
            point.x = Mathf.Clamp(point.x, GlobalVariables.mapMin.x, GlobalVariables.mapMax.x);
            point.z = Mathf.Clamp(point.z, GlobalVariables.mapMin.z, GlobalVariables.mapMax.z);

            // Adjust Y to terrain height
            point.y = TerrainManager.Instance.GetTerrainHeight(point);

            newPositions.Add(point);
        }

        return newPositions;
    }

    public void ActivateAnimals()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        TryRepopulate();

        herdIsActive = true;

        foreach (var animal in animalsInHerd)
        {
            animal.SetAIEnabled(true);

            Vector3 pos = GetRandomPointInRadius();
            animal.transform.position = pos;

            animal.transform.rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);

            Vector3 currentScale = animal.transform.localScale;

            var netTransform = animal.GetComponent<NetworkTransform>();
            if (netTransform != null)
                netTransform.Teleport(pos, animal.transform.rotation, currentScale);
        }

        // Enable visuals on clients
        SetAnimalStateClientRpc(true);
    }

    public void DeactivateAnimals()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        herdIsActive = false;

        foreach (var animal in animalsInHerd)
        {
            animal.SetAIEnabled(false);
        }

        SetAnimalStateClientRpc(false);
    }

    [ClientRpc]
    private void SetAnimalStateClientRpc(bool visualsEnabled)
    {
        foreach (var animal in animalsInHerd)
        {
            animal.animal.SetVisualsEnabled(visualsEnabled);

            if (visualsEnabled)
            {
                if (animal.trampler != null)
                {
                    animal.trampler.RegisterTrampler();
                }
            }
            else
            {
                if (animal.trampler != null)
                {
                    animal.trampler.UnregisterTrampler();
                }
            }
        }
    }

    public Vector3 GetRandomMovementPoint(float moveRadius)
    {
        const int maxAttempts = 10; // try up to 10 times for a valid position
        Vector3 point = Vector3.zero;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Random polar coordinates
            float r = moveRadius * Mathf.Sqrt(Random.value);
            float theta = Random.value * Mathf.PI * 2f;

            point.x = transform.position.x + r * Mathf.Cos(theta);
            point.z = transform.position.z + r * Mathf.Sin(theta);

            // Check if within map bounds
            if (point.x >= GlobalVariables.mapMin.x && point.x <= GlobalVariables.mapMax.x &&
                point.z >= GlobalVariables.mapMin.z && point.z <= GlobalVariables.mapMax.z)
            {
                point.y = TerrainManager.Instance.GetTerrainHeight(point);
                return point;
            }
        }

        // --- Fallback: Clamp if all attempts fail ---
        point.x = Mathf.Clamp(point.x, GlobalVariables.mapMin.x, GlobalVariables.mapMax.x);
        point.z = Mathf.Clamp(point.z, GlobalVariables.mapMin.z, GlobalVariables.mapMax.z);
        point.y = TerrainManager.Instance.GetTerrainHeight(point);

        return point;
    }

    public bool IsActive()
    {
        return herdIsActive;
    }

    private void HandleActivateDeactivate()
    {
        if (animalsInHerd.Count == 0)
            return;

        // Find nearest player distance
        float closestPlayerDist = float.MaxValue;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            GameObject playerObj = client.PlayerObject?.gameObject;
            if (playerObj == null) continue;

            float dist = Vector3.Distance(transform.position, playerObj.transform.position);
            if (dist < closestPlayerDist)
                closestPlayerDist = dist;
        }

        if (IsActive())
        {
            // Deactivate if all players are too far
            if (closestPlayerDist > activationDistance + deactivationOffset)
            {
                DeactivateAnimals();
            }
        }
        else
        {
            // Activate if at least one player is close
            if (closestPlayerDist <= activationDistance)
            {
                ActivateAnimals();
            }
        }
    }
}
