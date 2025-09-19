using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class Herd : MonoBehaviour
{
    public readonly List<AnimalAI> animalsInHerd = new List<AnimalAI>();
    public float radius = 10f;

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
        foreach (var animal in animalsInHerd)
        {
            animal.gameObject.SetActive(true);
            animal.transform.position = GetRandomPointInRadius();
        }
    }

    public void DeactivateAnimals()
    {
        foreach (var animal in animalsInHerd)
        {
            animal.gameObject.SetActive(false);
        }
    }

    private void HandleActivateDeactivate()
    {
        // IF ACTIVE: Check to see if all animals in herd are too far from player, if so deactivate.  

        // IF NOT ACTIVE: Check to see if herd position is close to player, if so activate.
    }
}
