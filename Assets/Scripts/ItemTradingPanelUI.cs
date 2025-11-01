using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemTradingPanelUI : MonoBehaviour
{
    public InventoryPanelUI inventoryPanel;
    public InventoryPanelUI otherInventoryPanel;
    public Inventory inventory;
    public Inventory otherInventory;
    public AmountUI amountUI;
    // public int amount;

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
        PopulateInventories(inventory, otherInventory);
    }

    public void OnItemSelectedOtherInventory(ItemInstance item)
    {
        TryTradeItem(otherInventory, inventory, item);
        PopulateInventories(inventory, otherInventory);
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

        // Remove only the transferred amount from the source
        itemSource.RemoveItem(item, amount);

        // Add the correct amount to the destination
        itemDestination.AddItem(tradedItem);
    }
}