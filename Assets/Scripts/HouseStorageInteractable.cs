using System.Collections.Generic;
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
            player.DropWorldItemServerRpc();
            player.GetStorageInventory().AddItem(itemToStore.GetItemData());
            itemToStore.DespawnItemServerRpc();
        }
        else
        {
            player.GetStorageMenu().OpenStorageMenu(player.GetInventory(), player.GetStorageInventory());
            player.GetStorageMenu().inventoryPanel.OnItemTooLarge = (item) =>
            {
                itemSpawner.DropItem(item, itemSpawner.transform.position, Vector3.zero);
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
}