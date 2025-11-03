using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LoadoutSlot : MonoBehaviour, IPointerClickHandler
{
    public WeaponClass allowedClass;
    private LoadoutMenu loadoutMenu;
    private ItemInstance assignedWeaponInstance;
    private Image icon;

    private void Awake()
    {
        icon = GetComponent<Image>();
    }

    public void Initialize(WeaponClass allowedClass, LoadoutMenu loadoutMenu)
    {
        this.allowedClass = allowedClass;
        this.loadoutMenu = loadoutMenu;
    }

    public bool IsEmpty => assignedWeaponInstance.Equals(default);

    public void AssignWeapon(ItemInstance weapon)
    {
        // Debug.Log("Weapon assigned: " + weapon);
        assignedWeaponInstance = weapon;
        Debug.Log("Weapon assigned: " + ItemDatabase.Instance.GetItem(weapon.key).icon);
        icon.sprite = ItemDatabase.Instance.GetItem(weapon.key).icon;
    }

    public void ClearSlot()
    {
        if (assignedWeaponInstance.Equals(default))
            return;

        assignedWeaponInstance = default;
        icon.sprite = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Simple click removes weapon
        if (!IsEmpty)
        {
            loadoutMenu.RemoveWeapon(assignedWeaponInstance);
            ClearSlot();
        }

    }

    public ItemInstance GetAssignedWeapon() => assignedWeaponInstance;
}
