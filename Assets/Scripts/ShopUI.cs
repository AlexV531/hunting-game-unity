using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public GameObject shopScreen;
    public Transform shopListContainer;
    public GameObject shopOptionPrefab;
    public Button closeButton;
    private Shop currentShop;
    private bool shopOpen = true;
    
    private void Start()
    {
        closeButton.onClick.AddListener(CloseShopScreen);

        CloseShopScreen();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenShopScreen(Shop shop)
    {
        shopScreen.SetActive(true);
        currentShop = shop;
        SetShopList(shop);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        shopOpen = true;
    }

    public void CloseShopScreen()
    {
        shopScreen.SetActive(false);
        currentShop = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        shopOpen = false;
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

    public bool IsShopOpen()
    {
        return shopOpen;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && IsShopOpen())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}