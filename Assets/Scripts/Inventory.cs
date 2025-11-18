using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    [SerializeField]
    private List<ItemInstance> items = new List<ItemInstance>();
    private int capacity = 0;
    private bool canHoldLargeItems = true;
    private WeaponManager weaponManager = null;

    public bool TryAddItem(ItemInstance newItem)
    {
        var def = ItemDatabase.Instance.GetItem(newItem.key);
        if (!canHoldLargeItems && def.itemSize == ItemSize.Large)
        {
            return false;
        }

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
                    return true;
                }
            }
        }

        if (capacity > 0)
        {
            if (items.Count < capacity)
            {
                items.Add(newItem);
                return true;
            }
        }
        else
        {
            items.Add(newItem);
            return true;
        }
        return false;
    }

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

    public bool IsItemTooLarge(ItemInstance newItem)
    {
        var def = ItemDatabase.Instance.GetItem(newItem.key);
        if (!canHoldLargeItems && def.itemSize == ItemSize.Large)
        {
            return true;
        }
        return false;
    }

    public int RemoveItem(ItemInstance target, int amount = 1)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Compare(target))
            {
                ItemInstance updated = items[i];
                int removedAmount = Mathf.Min(amount, updated.stackSize);
                updated.stackSize -= removedAmount;

                if (updated.stackSize <= 0)
                {
                    items.RemoveAt(i);
                    if (weaponManager != null)
                    {
                        weaponManager.RemoveFromLoadout(target);
                    }
                }
                else
                {
                    items[i] = updated;
                }

                return removedAmount;
            }
        }

        return 0;
    }

    public void Clear()
    {
        // If there's a WeaponManager, remove all weapons first
        if (weaponManager != null)
        {
            foreach (var item in items)
            {
                var def = ItemDatabase.Instance.GetItem(item.key);
                if (def is WeaponDefinition)
                {
                    weaponManager.RemoveFromLoadout(item);
                }
            }
        }

        // Clear the items list
        items.Clear();
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

    public ItemInstance GetInstance(int key)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].key == key)
                return items[i];
        }
        return default;
    }

    public ItemInstance GetInstance(ItemInstance itemInstance)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Compare(itemInstance))
                return items[i];
        }
        return default;
    }

    public List<ItemInstance> GetItems() => items;

    public int GetCapacity() => capacity;

    public void SetCapacity(int newCapacity) => capacity = newCapacity;

    public void SetCanHoldLargeItems(bool canHoldLargeItems) => this.canHoldLargeItems = canHoldLargeItems;

    public void SetWeaponManager(WeaponManager weaponManager) => this.weaponManager = weaponManager;
}
