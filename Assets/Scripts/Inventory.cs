using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    [SerializeReference]
    private List<ItemInstance> items = new List<ItemInstance>();

    public void AddItem(ItemInstance newItem)
    {
        foreach (var existing in items)
        {
            if (existing.Compare(newItem))
            {
                existing.stackSize += newItem.stackSize;
                return;
            }
        }

        items.Add(newItem);
    }

    public bool RemoveItem(ItemInstance target, int amount = 1)
    {
        for (int i = 0; i < items.Count; i++)
        {
            // Check if this is the exact instance
            if (items[i] == target)
            {
                items[i].stackSize -= amount;

                if (items[i].stackSize <= 0)
                    items.RemoveAt(i);

                return true;
            }
        }

        return false;
    }

    public List<ItemInstance> GetItems() => items;
}
