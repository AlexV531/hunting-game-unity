using UnityEngine;
using UnityEngine.UI;

public class ShopUI : UIMenu
{
    public Transform shopListContainer;
    public GameObject shopOptionPrefab;
    public Button closeButton;
    private Shop currentShop;
    
    protected override void Start()
    {
        base.Start();

        closeButton.onClick.AddListener(CloseMenu);
    }

    public void OpenShopMenu(Shop shop)
    {
        base.OpenMenu();

        currentShop = shop;
        SetShopList(shop);
    }

    public override void CloseMenu()
    {
        base.CloseMenu();

        currentShop = null;
    }

    private void SetShopList(Shop shop)
    {
        if (FirstPersonController.LocalPlayer == null)
            return;

        // Clear old buttons
        foreach (Transform child in shopListContainer)
        {
            Destroy(child.gameObject);
        }

        // Rebuild from current weapon list
        foreach (var purchasableItemKey in shop.purchasableItemKeys)
        {
            GameObject btnObj = Instantiate(shopOptionPrefab, shopListContainer);
            ShopButton btn = btnObj.GetComponent<ShopButton>();
            btn.Initialize(ItemDatabase.Instance.GetItem(purchasableItemKey), shop);
        }
    }
}