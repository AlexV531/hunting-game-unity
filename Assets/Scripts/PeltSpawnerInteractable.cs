using Unity.Netcode;
using UnityEngine;

public class PeltSpawnerInteractable : InteractableBase
{
    public ItemSpawner itemSpawner;

    public override void Interact(FirstPersonController player)
    {
        SpawnPeltServerRpc();
    }
    
    [ServerRpc (RequireOwnership = false)]
    public void SpawnPeltServerRpc()
    {
        ItemInstance pelt = new ItemInstance
        {
            key = 20,
            stackSize = 1,
            customData = new ItemCustomData
            {
                quality = 1f,
                color = Color.brown,
                description = "Brown"
            }
        };
        Debug.Log("Dropping item");
        itemSpawner.DropItem(pelt, itemSpawner.transform.position, Vector3.zero);
    }
}