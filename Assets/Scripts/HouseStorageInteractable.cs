using Unity.Netcode;
using UnityEngine;

public class HouseStorageInteractable : InteractableBase
{
    public ItemSpawner itemSpawner;

    public override void Interact(FirstPersonController player)
    {
        if (player.GetCarriedWorldItem() != null)
        {
            WorldItem itemToStore = player.GetCarriedWorldItem();

            if (player.GetStorageInventory().TryAddItem(itemToStore.GetItemData()))
            {
                player.DropWorldItemServerRpc();
                itemToStore.DespawnItemServerRpc();
            }
        }
        else
        {
            player.GetStorageMenu().OpenStorageMenu(player.GetInventory(), player.GetStorageInventory());
            player.GetStorageMenu().inventoryPanel.OnItemTooLarge = (item) =>
            {
                DropItemServerRpc(item);
            };
        }
    }

    public override string GetPrompt(FirstPersonController player)
    {
        if (player.GetCarriedWorldItem() != null)
        {
            return "Press \"e\" to store carried item";
        }
        else
        {
            return "Press \"e\" to store items";
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DropItemServerRpc(ItemInstance droppedItem)
    {
		itemSpawner.DropItem(droppedItem, itemSpawner.transform.position, Vector3.zero);
    }
}