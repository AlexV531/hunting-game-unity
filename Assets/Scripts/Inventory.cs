using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    [SerializeField]
    private List<ItemInstance> items = new List<ItemInstance>();

    public void AddItem(ItemInstance newItem)
    {
        var def = ItemDatabase.Instance.GetItem(newItem.key);
        if (def != null && def.stackable)
        {
            // Use index-based loop so we can modify the list element directly
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Compare(newItem))
                {
                    ItemInstance updated = items[i];
                    updated.stackSize += newItem.stackSize;
                    items[i] = updated; // write back the modified struct
                    return;
                }
            }
        }

        items.Add(newItem);
    }

    public bool RemoveItem(ItemInstance target, int amount = 1)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Equals(target))
            {
                ItemInstance updated = items[i];
                updated.stackSize -= amount;

                if (updated.stackSize <= 0)
                {
                    items.RemoveAt(i);
                }
                else
                {
                    items[i] = updated; // write-back mutated struct
                }

                return true;
            }
        }

        return false;
    }

    public List<ItemInstance> GetWeapons()
    {
        List<ItemInstance> weaponList = new List<ItemInstance>();

        for (int i = 0; i < items.Count; i++)
        {
            ItemDefinition def = ItemDatabase.Instance.GetItem(items[i].key);
            if (def is WeaponDefinition weaponDef)
            {
                if (weaponDef.contextual)
                    continue;

                weaponList.Add(items[i]);
            }
        }

        return weaponList;
    }

    public List<ItemInstance> GetItems() => items;
}
