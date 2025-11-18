using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class LoadoutSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    public Image icon;
    public TMP_Text nameText;
    public Image slotBackground;
    public CanvasGroup slotCanvasGroup; // Optional: for fading out invalid slots
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color greyedOutColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    public enum DragState { Normal, Valid, Invalid, Occupied }
    private DragState currentDragState = DragState.Normal;
    
    private ItemInstance currentItem;
    private ItemType itemType;
    private LoadoutMenu menu;

    public bool IsEmpty => currentItem.Equals(default);
    public ItemType ItemType => itemType;
    public ItemInstance CurrentWeapon => currentItem;

    public void Initialize(ItemType itemType, LoadoutMenu loadoutMenu)
    {
        this.itemType = itemType;
        menu = loadoutMenu;
        ClearSlot();
    }

    public void AssignItem(ItemInstance item)
    {
        if (item.Equals(default))
            return;

        currentItem = item;
        var def = ItemDatabase.Instance.GetItem(item.key);
        
        if (icon != null)
        {
            icon.sprite = def.icon;
            icon.enabled = true;
        }
        
        if (nameText != null)
            nameText.text = def.itemName;
    }

    public void ClearSlot()
    {
        currentItem = default;
        
        if (icon != null)
            icon.enabled = false;
        
        if (nameText != null)
            nameText.text = "";
        
        ResetVisuals();
    }

    public void SetDragState(DragState state)
    {
        currentDragState = state;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (slotBackground == null) return;

        slotBackground.color = currentDragState switch
        {
            DragState.Valid => normalColor,
            DragState.Invalid => greyedOutColor,
            _ => normalColor
        };
    }

    // Handle drops from item buttons
    public void OnDrop(PointerEventData eventData)
    {
        var draggedButton = eventData.pointerDrag?.GetComponent<ItemButton>();
        
        if (draggedButton != null)
        {
            // Dropping from item list
            // menu.TryAddWeaponToSlot(draggedButton.GetWeapon(), this);
        }
    }

    private void ResetVisuals()
    {
        if (slotBackground != null)
            slotBackground.color = normalColor;
    }

    // Optional: Visual feedback when hovering
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotBackground == null) return;
        
        var draggedButton = eventData.pointerDrag?.GetComponent<ItemButton>();
        
        if (draggedButton != null)
        {
            var def = WeaponDatabase.GetWeapon(draggedButton.GetItem().key);
            if (def.itemType == itemType && IsEmpty)
            {
                slotBackground.color = highlightColor;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateVisuals();
    }

    // Click to clear slot
    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (!IsEmpty)
        {
            menu.RemoveWeapon(currentItem);
            ClearSlot();
        }
    }
}
