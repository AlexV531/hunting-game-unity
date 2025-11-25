using Unity.Netcode;
using UnityEngine;

public class AnimalReward : NetworkBehaviour
{
    public bool butcherable;
    public bool skinned;
    public AnimalVariator animalVariator;
    public ItemSpawner itemSpawner;

    [Header("Meat Settings")]
    public int minMeat = 2;
    public int maxMeat = 4;

    [ServerRpc(RequireOwnership = false)]
    public void ButcherServerRpc(ulong clientId)
    {
        Debug.Log("Inside rpc");
        if (butcherable && !skinned)
        {
            SkinAnimal();
            skinned = true;
        }
        else if (butcherable && skinned)
        {
            ButcherAnimal(clientId);
        }
    }

    private void SkinAnimal()
    {
        // Create pelt item instance
        ItemInstance pelt = new ItemInstance
        {
            key = 20,
            stackSize = 1,
            customData = new ItemCustomData
            {
                quality = 1f,
                color = animalVariator.GetPelt().Color,
                description = animalVariator.GetPelt().Description
            }
        };
        itemSpawner.DropItem(pelt, transform.position, Vector3.zero);
    }

    private void ButcherAnimal(ulong clientId)
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            if (netObj.IsSceneObject == true)
            {
                // For in-scene objects — unspawn but keep the GameObject
                netObj.Despawn(false);
                gameObject.SetActive(false);
            }
            else
            {
                // For runtime-spawned animals — despawn and destroy completely
                netObj.Despawn(true);
            }
        }
        else
        {
            Destroy(gameObject); // fallback safety
        }

        // Calculate meat amount based on scale factor
        int meatAmount = CalculateMeatAmount();

        // Create meat item instance
        ItemInstance meat = new ItemInstance
        {
            key = 25,
            stackSize = meatAmount,
            customData = new ItemCustomData
            {
                quality = 1f
            }
        };
        itemSpawner.DropItem(meat, transform.position, Vector3.zero);

        // Reward the player who did the butchering
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var player = client.PlayerObject.GetComponent<FirstPersonController>();
            player.Money += 5;
        }

        Debug.Log($"Animal butchered by client {clientId}, meat dropped: {meatAmount}");
    }

    private int CalculateMeatAmount()
    {
        if (animalVariator == null)
        {
            Debug.LogWarning("AnimalVariator is null, using minimum meat amount");
            return minMeat;
        }

        float scaleFactor = animalVariator.scaleFactor;
        float minScale = animalVariator.minScale;
        float maxScale = animalVariator.maxScale;

        // Normalize the scale factor between 0 and 1 based on min/max scale
        float normalizedScale = Mathf.InverseLerp(minScale, maxScale, scaleFactor);

        // Interpolate between minMeat and maxMeat
        float meatFloat = Mathf.Lerp(minMeat, maxMeat, normalizedScale);

        // Round to nearest integer
        int meatAmount = Mathf.RoundToInt(meatFloat);

        Debug.Log($"Scale: {scaleFactor:F2}, Normalized: {normalizedScale:F2}, Meat: {meatAmount}");

        return meatAmount;
    }
}