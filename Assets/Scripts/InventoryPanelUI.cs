using UnityEngine;
using System.Collections.Generic;

public class InventoryPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private Transform contentParent;

    public void PopulateInventory(List<ItemInstance> items)
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var item in items)
        {
            var slotObj = Instantiate(itemSlotPrefab, contentParent);
            var slot = slotObj.GetComponent<ItemSlot>();
            slot.SetItem(item);
        }
    }
}
