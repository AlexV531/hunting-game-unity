using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class LoadoutSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    public Image weaponIcon;
    public TMP_Text weaponNameText;
    public Image slotBackground;
    public CanvasGroup slotCanvasGroup; // Optional: for fading out invalid slots
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    // public Color invalidColor = Color.red;
    public Color greyedOutColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color validDropColor = Color.white;
    
    public enum DragState { Normal, Valid, Invalid, Occupied }
    private DragState currentDragState = DragState.Normal;
    
    private ItemInstance currentWeapon;
    private WeaponClass weaponClass;
    private LoadoutMenu menu;

    public bool IsEmpty => currentWeapon.Equals(default);
    public WeaponClass WeaponClass => weaponClass;
    public ItemInstance CurrentWeapon => currentWeapon;

    private void Awake()
    {
    }

    public void Initialize(WeaponClass wClass, LoadoutMenu loadoutMenu)
    {
        weaponClass = wClass;
        menu = loadoutMenu;
        ClearSlot();
    }

    public void AssignWeapon(ItemInstance weapon)
    {
        currentWeapon = weapon;
        var def = WeaponDatabase.GetWeapon(weapon.key);
        
        if (weaponIcon != null)
        {
            weaponIcon.sprite = def.icon;
            weaponIcon.enabled = true;
        }
        
        if (weaponNameText != null)
            weaponNameText.text = def.itemName;
    }

    public void ClearSlot()
    {
        currentWeapon = default;
        
        if (weaponIcon != null)
            weaponIcon.enabled = false;
        
        if (weaponNameText != null)
            weaponNameText.text = "";
        
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
            DragState.Valid => validDropColor,
            DragState.Invalid => greyedOutColor,
            _ => normalColor
        };
    }

    // Handle drops from weapon buttons
    public void OnDrop(PointerEventData eventData)
    {
        var draggedButton = eventData.pointerDrag?.GetComponent<WeaponButton>();
        
        if (draggedButton != null)
        {
            // Dropping from weapon list
            menu.TryAddWeaponToSlot(draggedButton.GetWeapon(), this);
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
        
        var draggedButton = eventData.pointerDrag?.GetComponent<WeaponButton>();
        
        if (draggedButton != null)
        {
            var def = WeaponDatabase.GetWeapon(draggedButton.GetWeapon().key);
            if (def.weaponClass == weaponClass && IsEmpty)
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
            menu.RemoveWeapon(currentWeapon);
            ClearSlot();
        }
    }
}
