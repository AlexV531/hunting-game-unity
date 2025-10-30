using UnityEngine;

public class WeaponPickup : InteractableBase
{
    public override void Interact(FirstPersonController player)
    {
        var inventory = player.GetInventory();
        
        // Check if weapon with key 0 already exists
        bool hasWeapon = false;
        foreach (var item in inventory.GetWeapons())
        {
            if (item.key == 1)
            {
                hasWeapon = true;
                break;
            }
        }

        // Only add weapon if player doesn't already have it
        if (!hasWeapon)
        {
            ItemInstance weapon = new ItemInstance()
            {
                key = 1,
                stackSize = 1
            };
            Debug.Log("Added item to inventory with key " + weapon.key);
            inventory.AddItem(weapon);
        }
    }
}