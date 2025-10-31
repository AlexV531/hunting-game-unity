using UnityEngine;

public class PlayerInventoryMenu : MonoBehaviour
{
    public GameObject playerInventoryMenu;
    public InventoryPanelUI inventoryPanel;
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
        inventoryPanel.PopulateInventory(FirstPersonController.LocalPlayer.GetInventory().GetItems());
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
}
