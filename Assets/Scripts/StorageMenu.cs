using UnityEngine.UI;

public class StorageMenu : UIMenu
{
    public ItemTradingPanelUI inventoryPanel;
    public Button closeButton;

    protected override void Start()
    {
        base.Start();

        closeButton.onClick.AddListener(CloseMenu);
    }

    public void OpenStorageMenu(Inventory inventory, Inventory otherInventory)
    {
        base.OpenMenu();

        inventoryPanel.PopulateInventories(
            inventory,
            otherInventory
        );
    }
}