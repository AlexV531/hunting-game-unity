using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopButton : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button button;

    private ItemDefinition item;
    private string shopOptionText;
    private int price;
    private Shop shop;

    public void Initialize(Sprite icon, string shopOptionText, int price, Shop shop)
    {
        this.shop = shop;
        this.shopOptionText = shopOptionText;
        this.price = price;

        this.icon.sprite = icon;
        nameText.text = shopOptionText;
        priceText.text = price.ToString();
        button.onClick.AddListener(OnClicked);
    }

    public void Initialize(ItemDefinition item, Shop shop)
    {
        this.shop = shop;
        this.item = item;
        
        shopOptionText = item.itemName;
        price = item.price;

        icon.sprite = item.icon;
        nameText.text = shopOptionText;
        priceText.text = price.ToString();
        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        Debug.Log(nameText.text + " clicked");
        if (FirstPersonController.LocalPlayer.Money >= price)
        {
            FirstPersonController.LocalPlayer.Money -= price;
            Purchase();
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }

    // [ServerRpc(RequireOwnership = false)]
    // private void PurchaseServerRpc()
    // {
    //     Debug.Log(nameText.text + " acquired");
    //     if (item != null)
    //     {
    //         item.Acquire(player);
    //     }
    // }

    private void Purchase() // Should protect behind server rpc at some point
    {
        Debug.Log(nameText.text + " acquired");
        if (item != null)
        {
            // item.Acquire(FirstPersonController.LocalPlayer);
            if (item is WeaponDefinition)
            {
                FirstPersonController.LocalPlayer.GetWeaponManager().UnlockWeapon(item.key);
            }
        }
    }
}
