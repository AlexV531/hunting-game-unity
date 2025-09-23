using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class Herd : MonoBehaviour
{
    public readonly List<AnimalAI> animalsInHerd = new List<AnimalAI>();
    public float radius = 10f;
    private float activationDistance = 200f;
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
            animalsInHerd.Add(animalAI);
    }

    public void UnregisterHerdAnimal(AnimalAI animalAI)
    {
        if (animalsInHerd.Contains(animalAI))
            animalsInHerd.Remove(animalAI);
    }

    public void InitializeAnimals(int numAnimals, GameObject animalPrefab)
    {
        for (int i = 0; i < numAnimals; i++)
        {
            GameObject animal = Instantiate(animalPrefab);
            RegisterHerdAnimal(animal.GetComponent<AnimalAI>());
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
        Debug.Log("Activating herd");

        foreach (var animal in animalsInHerd)
        {
            animal.SetAIEnabled(true);
            animal.transform.position = GetRandomPointInRadius();
        }

        SetAnimalStateClientRpc(true);
        herdIsActive = true;
    }

    public void DeactivateAnimals()
    {
        Debug.Log("Deactivating herd");

        foreach (var animal in animalsInHerd)
        {
            animal.SetAIEnabled(false);
        }

        SetAnimalStateClientRpc(false);
        herdIsActive = false;
    }

    [ClientRpc]
    public void SetAnimalStateClientRpc(bool visualsEnabled)
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
