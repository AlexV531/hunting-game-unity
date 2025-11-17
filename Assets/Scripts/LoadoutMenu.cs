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
    private ItemInstance currentlyDraggedWeapon;

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

    // Called when a weapon is dropped onto a valid slot
    public bool TryAddWeaponToSlot(ItemInstance weapon, LoadoutSlot targetSlot)
    {
        var def = WeaponDatabase.GetWeapon(weapon.key);
        
        // Check if slot accepts this weapon class
        if (targetSlot.WeaponClass != def.weaponClass)
            return false;
        
        var targetList = editableLoadout.GetListForClass(def.weaponClass);
        
        // Check if weapon is already in loadout
        bool alreadyInLoadout = targetList.Exists(w => w.Compare(weapon));
        
        // If not already in loadout, check capacity (only if slot is empty)
        if (!alreadyInLoadout && targetSlot.IsEmpty && targetList.Count >= editableLoadout.GetMaxCountForClass(def.weaponClass))
            return false;
        
        // If weapon is already in loadout, remove it from its old slot
        if (alreadyInLoadout)
        {
            // Find and clear the old slot
            var slots = def.weaponClass switch
            {
                WeaponClass.Large => largeSlots,
                WeaponClass.Small => smallSlots,
                WeaponClass.Tool => toolSlots,
                _ => null
            };
            
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.CurrentWeapon.Compare(weapon))
                {
                    slot.ClearSlot();
                    break;
                }
            }
            
            targetList.RemoveAll(w => w.Compare(weapon));
        }
        
        // If slot is occupied, remove the old weapon first
        if (!targetSlot.IsEmpty)
        {
            Debug.Log("Replacing weapon in slot");
            var oldWeapon = targetSlot.CurrentWeapon;
            targetList.RemoveAll(w => w.Compare(oldWeapon));
        }
        
        targetSlot.AssignWeapon(weapon);
        targetList.Add(weapon);
        return true;
    }

    public void OnWeaponDragStart(ItemInstance weapon)
    {
        currentlyDraggedWeapon = weapon;
        var def = WeaponDatabase.GetWeapon(weapon.key);
        
        // Update all slots based on whether they can accept this weapon
        UpdateSlotsForDrag(largeSlots, def.weaponClass == WeaponClass.Large);
        UpdateSlotsForDrag(smallSlots, def.weaponClass == WeaponClass.Small);
        UpdateSlotsForDrag(toolSlots, def.weaponClass == WeaponClass.Tool);
    }

    public void OnWeaponDragEnd()
    {
        currentlyDraggedWeapon = default;
        
        // Reset all slots to normal
        foreach (var slot in largeSlots) slot.SetDragState(LoadoutSlot.DragState.Normal);
        foreach (var slot in smallSlots) slot.SetDragState(LoadoutSlot.DragState.Normal);
        foreach (var slot in toolSlots) slot.SetDragState(LoadoutSlot.DragState.Normal);
    }

    private void UpdateSlotsForDrag(List<LoadoutSlot> slots, bool isValidClass)
    {
        foreach (var slot in slots)
        {
            if (!isValidClass)
            {
                slot.SetDragState(LoadoutSlot.DragState.Invalid);
            }
            else if (slot.IsEmpty)
            {
                slot.SetDragState(LoadoutSlot.DragState.Valid);
            }
            else
            {
                slot.SetDragState(LoadoutSlot.DragState.Occupied);
            }
        }
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
