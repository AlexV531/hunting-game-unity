using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public TMP_Text countText;
    public TMP_Text nameText;
    public Button button;
    private ItemInstance item;

    public void SetItem(ItemInstance newItem, UnityAction<ItemInstance> onClickAction)
    {
        item = newItem;

        button.onClick.RemoveAllListeners();

        // Add the new listener
        if (onClickAction != null)
            button.onClick.AddListener(() => onClickAction(item));

        if (item.Equals(default))
        {
            icon.sprite = null;
            icon.enabled = false;
            countText.text = "";
            nameText.text = "";
            return;
        }
        ItemDefinition def = ItemDatabase.Instance.GetItem(item.key);
        icon.sprite = def.icon;
        icon.enabled = true;
        nameText.text = def.itemName;

        countText.text = item.stackSize > 1 ? item.stackSize.ToString() : "";
    }
}