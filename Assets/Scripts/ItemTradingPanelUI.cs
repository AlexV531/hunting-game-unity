using System;
using UnityEngine;
using UnityEngine.Events;

public class ItemTradingPanelUI : MonoBehaviour
{
    public InventoryPanelUI inventoryPanel;
    public InventoryPanelUI otherInventoryPanel;
    public Inventory inventory;
    public Inventory otherInventory;
    public AmountUI amountUI;
    public UnityAction<ItemInstance> OnItemTooLarge;

    public void PopulateInventories(Inventory inventory, Inventory otherInventory)
    {
        this.inventory = inventory;
        inventoryPanel.PopulateInventory(
            inventory.GetItems(),
            (clickedItem) =>
            {
                Debug.Log("Clicked on item with key: " + clickedItem.key);
                OnItemSelectedInventory(clickedItem);
            }
        );

        this.otherInventory = otherInventory;
        otherInventoryPanel.PopulateInventory(
            otherInventory.GetItems(),
            (clickedItem) =>
            {
                Debug.Log("Clicked on item with key: " + clickedItem.key);
                OnItemSelectedOtherInventory(clickedItem);
            }
        );
    }

    public void OnItemSelectedInventory(ItemInstance item)
    {
        TryTradeItem(inventory, otherInventory, item);
    }

    public void OnItemSelectedOtherInventory(ItemInstance item)
    {
        TryTradeItem(otherInventory, inventory, item);
    }

    public void TryTradeItem(Inventory itemSource, Inventory itemDestination, ItemInstance item)
    {
        if (item.stackSize > 1)
        {
            amountUI.OpenPrompt(item.stackSize, (amount) =>
            {
                TradeItem(itemSource, itemDestination, item, amount);
            });
        }
        else
        {
            TradeItem(itemSource, itemDestination, item, 1);
        }
    }

    private void TradeItem(Inventory itemSource, Inventory itemDestination, ItemInstance item, int amount)
    {
        int tradableAmount = Math.Clamp(amount, 1, item.stackSize);

        // Create a copy with the correct stack size for the destination
        ItemInstance tradedItem = item;
        tradedItem.stackSize = tradableAmount;

        if (itemDestination.IsItemTooLarge(tradedItem))
        {
            Debug.Log("Item too large for player inventory, doing unity action");
            if (OnItemTooLarge != null)
            {
                OnItemTooLarge.Invoke(tradedItem);
                itemSource.RemoveItem(item, tradableAmount);
            }
            return;
        }

        // Try adding the item, if inventory at capacity, do not remove item
        if (itemDestination.TryAddItem(tradedItem))
        {
            // Remove only the transferred amount from the source
            itemSource.RemoveItem(item, amount);
        }
        PopulateInventories(inventory, otherInventory);
    }
}