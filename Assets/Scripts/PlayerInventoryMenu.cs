using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerInventoryMenu : MonoBehaviour
{
    public GameObject playerInventoryMenu;
    public InventoryPanelUI inventoryPanel;
    public InventoryPanelUI onShoulderInventoryPanel;
    public TMP_Text itemInfoTitleText;
    public Transform itemInfoContent;
    public InspectRoom inspectRoom;
    public GameObject itemCustomTextPrefab;
    private static bool playerInventoryOpen = false;

    private void Awake()
    {
        playerInventoryMenu.SetActive(false);
    }

    public void ClosePlayerInventoryMenu()
    {
        playerInventoryMenu.SetActive(false);
        CursorManager.SetCursorActive(false);
        playerInventoryOpen = false;
    }

    public void OpenPlayerInventoryMenu()
    {
        playerInventoryMenu.SetActive(true);
        // inventoryPanel.PopulateInventory(FirstPersonController.LocalPlayer.GetInventory().GetItems());
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

        ClearItemInfo();

        CursorManager.SetCursorActive(true);
        playerInventoryOpen = true;
    }

    public bool IsPlayerInventoryOpen()
    {
        return playerInventoryOpen;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && IsPlayerInventoryOpen())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OnItemSelected(ItemInstance item)
    {
        // Clear previous info first
        ClearItemInfo();

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
        itemInfoTitleText.text = "";
        foreach (Transform child in itemInfoContent)
        {
            Destroy(child.gameObject);
        }
        inspectRoom.ClearInspectTarget();
    }
}
