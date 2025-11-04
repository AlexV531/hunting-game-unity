using UnityEngine;

public class ItemPickup : InteractableBase
{
    public int itemKey;
    public int amount;

    public override void Interact(FirstPersonController player)
    {
        var inventory = player.GetInventory();

        // Check if item exists in player's inventory
        bool hasItem = false;

        ItemInstance itemPickup = new ItemInstance()
        {
            key = itemKey,
            stackSize = amount
        };

        foreach (var item in inventory.GetItems())
        {
            if (item.key == itemPickup.key)
            {
                hasItem = true;
                break;
            }
        }

        // Only add weapon if player doesn't already have it
        if (!hasItem)
        {
            Debug.Log("Added item to inventory with key " + itemPickup.key);
            inventory.TryAddItem(itemPickup);
        }
    }
}