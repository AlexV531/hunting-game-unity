using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LoadoutSlot : MonoBehaviour, IPointerClickHandler
{
    public WeaponClass allowedClass;
    private LoadoutManager manager;
    private ItemInstance assignedWeaponInstance;
    private Image icon;

    private void Awake()
    {
        icon = GetComponent<Image>();
    }

    public void Initialize(WeaponClass allowedClass, LoadoutManager manager)
    {
        this.allowedClass = allowedClass;
        this.manager = manager;
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

        // Remove from the manager’s loadout list
        manager.RemoveWeaponFromLoadout(assignedWeaponInstance);
        assignedWeaponInstance = default;
        icon.sprite = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Simple click removes weapon
        if (!IsEmpty)
            ClearSlot();
    }

    public ItemInstance GetAssignedWeapon() => assignedWeaponInstance;
}
