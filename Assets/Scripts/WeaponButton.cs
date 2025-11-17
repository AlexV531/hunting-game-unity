using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WeaponButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TMP_Text weaponNameText;
    public Image weaponIcon;
    
    private ItemInstance weaponItem;
    private LoadoutMenu menu;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;

    public void Initialize(ItemInstance item, LoadoutMenu loadoutMenu)
    {
        weaponItem = item;
        menu = loadoutMenu;
        
        var def = WeaponDatabase.GetWeapon(item.key);
        if (weaponNameText != null)
            weaponNameText.text = def.itemName;
        if (weaponIcon != null && def.icon != null)
            weaponIcon.sprite = def.icon;
        
        // Setup components
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = transform.position;

        menu.OnWeaponDragStart(weaponItem);
        
        // Move to root canvas for proper rendering
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
        
        // Make semi-transparent while dragging
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        menu.OnWeaponDragEnd();

        // Check what we dropped on
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool droppedOnSlot = false;
        foreach (var result in results)
        {
            var slot = result.gameObject.GetComponent<LoadoutSlot>();
            if (slot != null)
            {
                droppedOnSlot = menu.TryAddWeaponToSlot(weaponItem, slot);
                break;
            }
        }

        // Return to original position
        transform.SetParent(originalParent);
        transform.position = originalPosition;
    }

    public ItemInstance GetWeapon()
    {
        return weaponItem;
    }
}
