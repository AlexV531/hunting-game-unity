using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

public class PlayerInventoryMenu : UIMenu
{
    public InventoryPanelUI inventoryPanel;
    public InventoryPanelUI onShoulderInventoryPanel;
    public TMP_Text itemInfoTitleText;
    public Transform itemInfoContent;
    public InspectRoom inspectRoom;
    public GameObject itemCustomTextPrefab;
    public AmountUI amountUI;
    private ItemInstance selectedItem;
    private PlayerInputs inputs;

    void Update()
    {
        if (IsMenuOpen() && inputs != null)
        {
            if (inputs.dropItem)
            {
                if (!selectedItem.Equals(default))
                {
                    TryDropItem(selectedItem);
                }
                inputs.dropItem = false;
            }
        }
    }

    public override void OpenMenu()
    {
        base.OpenMenu();

        if (inputs != null)
        {
            inputs.dropItem = false;
        }

        PopulateInventory();

        ClearItemInfo();
    }
    
    public void PopulateInventory()
    {
        inventoryPanel.PopulateInventory(
            FirstPersonController.LocalPlayer.GetInventory().GetItems(),
            (clickedItem) =>
            {
                Debug.Log("Clicked on item with key: " + clickedItem.key);
                OnItemSelected(clickedItem);
            }
        );

        WorldItem shoulderItem = FirstPersonController.LocalPlayer.GetCarriedWorldItem();

        if (shoulderItem != null)
        {
            List<ItemInstance> onShoulderItems = new List<ItemInstance>
            {
                shoulderItem.GetItemData()
            };

            // On Shoulder slot
            onShoulderInventoryPanel.PopulateInventory(
                onShoulderItems,
                (clickedItem) =>
                {
                    Debug.Log("Clicked on item with key: " + clickedItem.key);
                    OnItemSelected(clickedItem);
                }
            );
        }
        else
        {
            onShoulderInventoryPanel.ClearInventory();
        }
    }

    public void OnItemSelected(ItemInstance item)
    {
        // Clear previous info first
        ClearItemInfo();

        selectedItem = item;

        // Get item definition
        ItemDefinition def = ItemDatabase.Instance.GetItem(item.key);
        if (def != null)
        {
            // Set the title text
            itemInfoTitleText.text = def.itemName;
            ItemType type = def.itemType;

            AddCustomItemText("Size: " + def.itemSize);

            if (type == ItemType.AnimalPelt)
            {
                AddCustomItemText("Color: " + item.customData.description);
                AddCustomItemText("Quality: " + item.customData.quality);
            }

            inspectRoom.ReplaceInspectTarget(def.worldAppearancePrefab, new Vector3(0, 0, -3));
        }
    }

    public void TryDropItem(ItemInstance item)
    {
        if (item.stackSize > 1)
        {
            amountUI.OpenPrompt(item.stackSize, (amount) =>
            {
                DropItem(item, amount);
            });
        }
        else
        {
            DropItem(item, 1);
        }
    }

    public void DropItem(ItemInstance item, int amount)
    {
        int droppableAmount = Math.Clamp(amount, 1, item.stackSize);

        // Create a copy with the correct stack size for the destination
        ItemInstance droppedItem = item;
        droppedItem.stackSize = droppableAmount;

        FirstPersonController.LocalPlayer.GetInventory().RemoveItem(item, amount);
        FirstPersonController.LocalPlayer.itemSpawner.DropItem(droppedItem, FirstPersonController.LocalPlayer.itemSpawner.transform.position, Vector3.zero);
        PopulateInventory();
    }
    
    public void AddCustomItemText(string customItemText)
    {
        if (itemCustomTextPrefab != null && itemInfoContent != null)
        {
            GameObject customTextObj = Instantiate(itemCustomTextPrefab, itemInfoContent);
            TMP_Text tmpText = customTextObj.GetComponent<TMP_Text>();
            if (tmpText != null)
            {
                // You can set any additional info you want here
                tmpText.text = customItemText; 
            }
        }
    }

    public void ClearItemInfo()
    {
        selectedItem = default;
        itemInfoTitleText.text = "";
        foreach (Transform child in itemInfoContent)
        {
            Destroy(child.gameObject);
        }
        inspectRoom.ClearInspectTarget();
    }

    public void SetPlayerInput(PlayerInputs inputs) => this.inputs = inputs;
}
