using UnityEngine;
using UnityEngine.UI;
using TMPro; // if you’re using TextMeshPro (recommended)

public class ItemSlot : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public TMP_Text countText;
    public TMP_Text nameText;

    private ItemInstance item;

    public void SetItem(ItemInstance newItem)
    {
        item = newItem;

        if (item == null)
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