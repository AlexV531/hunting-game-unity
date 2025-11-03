using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LoadoutMenu : UIMenu
{
    [Header("UI References")]
    public Transform weaponListContainer;
    public GameObject weaponButtonPrefab;
    public Button confirmButton;

    [Header("Slot Containers")]
    public List<LoadoutSlot> largeSlots;
    public List<LoadoutSlot> smallSlots;
    public List<LoadoutSlot> toolSlots;

    private Loadout editableLoadout;

    protected override void Start()
    {
        base.Start();

        confirmButton.onClick.AddListener(OnLoadoutConfirmed);

        foreach (var s in largeSlots) s.Initialize(WeaponClass.Large, this);
        foreach (var s in smallSlots) s.Initialize(WeaponClass.Small, this);
        foreach (var s in toolSlots) s.Initialize(WeaponClass.Tool, this);
    }

    public override void OpenMenu()
    {
        base.OpenMenu();

        if (FirstPersonController.LocalPlayer == null)
            return;

        // Copy the loadout from WeaponManager for editing
        editableLoadout = FirstPersonController.LocalPlayer.GetWeaponManager().GetCurrentLoadout().Clone();

        RefreshWeaponList();
        RefreshLoadoutSlots();
    }

    private void RefreshWeaponList()
    {
        foreach (Transform child in weaponListContainer)
            Destroy(child.gameObject);

        var inventory = FirstPersonController.LocalPlayer.GetInventory();
        foreach (var weaponItem in inventory.GetWeapons())
        {
            var def = WeaponDatabase.GetWeapon(weaponItem.key);
            if (def.contextual) continue;

            var btnObj = Instantiate(weaponButtonPrefab, weaponListContainer);
            var btn = btnObj.GetComponent<WeaponButton>();
            btn.Initialize(weaponItem, this);
        }
    }

    public void RefreshLoadoutSlots()
    {
        foreach (var slot in largeSlots) slot.ClearSlot();
        foreach (var slot in smallSlots) slot.ClearSlot();
        foreach (var slot in toolSlots) slot.ClearSlot();

        foreach (var w in editableLoadout.largeWeapons)
            GetFreeSlotForClass(WeaponClass.Large)?.AssignWeapon(w);

        foreach (var w in editableLoadout.smallWeapons)
            GetFreeSlotForClass(WeaponClass.Small)?.AssignWeapon(w);

        foreach (var w in editableLoadout.tools)
            GetFreeSlotForClass(WeaponClass.Tool)?.AssignWeapon(w);
    }

    public void OnWeaponSelected(ItemInstance weapon)
    {
        var def = WeaponDatabase.GetWeapon(weapon.key);
        var targetList = editableLoadout.GetListForClass(def.weaponClass);

        // Prevent duplicates
        if (targetList.Exists(w => w.Compare(weapon)))
            return;

        if (targetList.Count >= editableLoadout.GetMaxCountForClass(def.weaponClass))
            return;

        var slot = GetFreeSlotForClass(def.weaponClass);
        if (slot == null)
            return;

        slot.AssignWeapon(weapon);
        targetList.Add(weapon);
    }

    public void RemoveWeapon(ItemInstance weapon)
    {
        var def = WeaponDatabase.GetWeapon(weapon.key);
        editableLoadout.GetListForClass(def.weaponClass).RemoveAll(w => w.Compare(weapon));
    }

    private LoadoutSlot GetFreeSlotForClass(WeaponClass wClass)
    {
        var slots = wClass switch
        {
            WeaponClass.Large => largeSlots,
            WeaponClass.Small => smallSlots,
            WeaponClass.Tool => toolSlots,
            _ => null
        };

        return slots?.Find(s => s.IsEmpty);
    }

    private void OnLoadoutConfirmed()
    {
        FirstPersonController.LocalPlayer.GetWeaponManager().SetUpLoadout(editableLoadout);
        CloseMenu();
    }
}
