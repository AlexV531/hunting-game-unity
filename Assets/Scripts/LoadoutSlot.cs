using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LoadoutSlot : MonoBehaviour, IPointerClickHandler
{
    public WeaponClass allowedClass;
    private LoadoutManager manager;
    private WeaponDefinition assignedWeapon;
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

    public bool IsEmpty => assignedWeapon == null;

    public void AssignWeapon(WeaponDefinition weapon)
    {
        Debug.Log("Weapon assigned: " + weapon.key);
        assignedWeapon = weapon;
        icon.sprite = weapon.icon;
    }

    public void ClearSlot()
    {
        if (assignedWeapon == null)
            return;

        // Remove from the manager’s loadout list
        manager.RemoveWeaponFromLoadout(assignedWeapon);
        assignedWeapon = null;
        icon.sprite = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Simple click removes weapon
        if (!IsEmpty)
            ClearSlot();
    }

    public WeaponDefinition GetAssignedWeapon() => assignedWeapon;
}
