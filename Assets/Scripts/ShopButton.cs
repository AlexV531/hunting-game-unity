using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopButton : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button button;

    private ItemInstance item;
    private string shopOptionText;
    private int price;
    private Shop shop;

    // public void Initialize(Sprite icon, string shopOptionText, int price, Shop shop)
    // {
    //     this.shop = shop;
    //     this.shopOptionText = shopOptionText;
    //     this.price = price;

    //     this.icon.sprite = icon;
    //     nameText.text = shopOptionText;
    //     priceText.text = price.ToString();
    //     button.onClick.AddListener(OnClicked);
    // }

    public void Initialize(ItemInstance item, Shop shop)
    {
        this.shop = shop;
        this.item = item;

        ItemDefinition def = ItemDatabase.Instance.GetItem(item.key);
        shopOptionText = def.itemName;
        price = def.price;

        icon.sprite = def.icon;
        nameText.text = shopOptionText;
        priceText.text = "$" + price.ToString();
        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        Debug.Log(nameText.text + " clicked");
        if (FirstPersonController.LocalPlayer.Money >= price)
        {
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
        if (item.Equals(default))
        {
            Debug.Log("No item set to purchase");
        }

        if (FirstPersonController.LocalPlayer.GetInventory().TryAddItem(item))
        {
            FirstPersonController.LocalPlayer.Money -= price;
            Debug.Log(nameText.text + " acquired");
        }
        else
        {
            Debug.Log("Not enough inventory space for " + nameText.text);
        }
    }
}
