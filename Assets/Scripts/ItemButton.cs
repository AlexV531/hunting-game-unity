using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TMP_Text nameText;
    public Image icon;
    
    private ItemInstance item;
    private LoadoutMenu menu;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;

    public void Initialize(ItemInstance item, LoadoutMenu loadoutMenu)
    {
        this.item = item;
        menu = loadoutMenu;
        
        var def = ItemDatabase.Instance.GetItem(item.key);
        if (nameText != null)
            nameText.text = def.itemName;
        if (icon != null && def.icon != null)
            icon.sprite = def.icon;
        
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

        menu.OnItemDragStart(item);
        
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
                droppedOnSlot = menu.TryAddItemToSlot(item, slot);
                break;
            }
        }

        // Return to original position
        transform.SetParent(originalParent);
        transform.position = originalPosition;
    }

    public ItemInstance GetItem()
    {
        return item;
    }
}
