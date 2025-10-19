using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;

public class Herd : MonoBehaviour
{
    public readonly List<AnimalAI> animalsInHerd = new List<AnimalAI>();
    public float radius = 10f;
    private float activationDistance = 400f;
    private float deactivationOffset = 100f;
    private bool herdIsActive = false;

    void Update()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

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

    public void InitializeAnimals(int numAnimals, GameObject animalPrefab)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("InitializeAnimals should only be called on the server.");
            return;
        }

        for (int i = 0; i < numAnimals; i++)
        {
            Vector3 spawnPos = GetRandomPointInRadius();

            GameObject animal = Instantiate(animalPrefab, spawnPos, Quaternion.identity);

            NetworkObject netObj = animal.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("Animal prefab must have a NetworkObject component!");
                Destroy(animal);
                continue;
            }

            // Spawn on the server to sync with clients
            netObj.Spawn();

            AnimalAI animalAI = animal.GetComponent<AnimalAI>();
            if (animalAI == null)
            {
                Debug.LogError("Animal prefab must have an AnimalAI component!");
                Destroy(animal);
                continue;
            }

            RegisterHerdAnimal(animalAI);
        }

        ActivateAnimals();
    }

    public void HerdFleeTo(List<Vector3> target_list)
    {
        transform.position = target_list[^1];
        for (int i = 0; i < animalsInHerd.Count; i++)
        {
            animalsInHerd[i].SetFleeing(GetRandomPointsInRadiusForArray(target_list));
        }
    }

    public Vector3 GetRandomPointInRadius()
    {
        // Random polar coordinates
        float r = radius * Mathf.Sqrt(Random.value);
        float theta = Random.value * Mathf.PI * 2f;

        Vector3 point = Vector3.zero;
        point.x = transform.position.x + r * Mathf.Cos(theta);
        point.z = transform.position.z + r * Mathf.Sin(theta);
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
            point.y = TerrainManager.Instance.GetTerrainHeight(point);

            newPositions.Add(point);
        }

        return newPositions;
    }

    public void ActivateAnimals()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        herdIsActive = true;

        foreach (var animal in animalsInHerd)
{
            animal.SetAIEnabled(true);

            Vector3 pos = GetRandomPointInRadius();
            animal.transform.position = pos;

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
        }
    }

    public bool isActive()
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

        if (isActive())
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
