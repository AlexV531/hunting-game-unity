using Unity.Netcode;
using UnityEngine;

public class AnimalReward : NetworkBehaviour
{
    public bool butcherable;
    public bool skinned;
    public AnimalVariator animalVariator;
    public ItemSpawner itemSpawner;

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
        Debug.Log("Making item " + animalVariator);
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
        Debug.Log("Dropping item");
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

        // Reward the player who did the butchering
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var player = client.PlayerObject.GetComponent<FirstPersonController>();
            player.Money += 5;
        }

        Debug.Log($"Animal butchered by client {clientId}");
    }
}