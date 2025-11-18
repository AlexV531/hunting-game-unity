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
        // Clear all slots first
        foreach (var slot in largeSlots) slot.ClearSlot();
        foreach (var slot in smallSlots) slot.ClearSlot();
        foreach (var slot in toolSlots) slot.ClearSlot();
        
        // Assign weapons from arrays to corresponding slots
        for (int i = 0; i < editableLoadout.largeWeapons.Length && i < largeSlots.Count; i++)
        {
            if (!editableLoadout.largeWeapons[i].Equals(default))
                largeSlots[i].AssignWeapon(editableLoadout.largeWeapons[i]);
        }
        
        for (int i = 0; i < editableLoadout.smallWeapons.Length && i < smallSlots.Count; i++)
        {
            if (!editableLoadout.smallWeapons[i].Equals(default))
                smallSlots[i].AssignWeapon(editableLoadout.smallWeapons[i]);
        }
        
        for (int i = 0; i < editableLoadout.tools.Length && i < toolSlots.Count; i++)
        {
            if (!editableLoadout.tools[i].Equals(default))
                toolSlots[i].AssignWeapon(editableLoadout.tools[i]);
        }
    }

    // Called when a weapon is dropped onto a valid slot
    public bool TryAddWeaponToSlot(ItemInstance weapon, LoadoutSlot targetSlot)
    {
        var def = WeaponDatabase.GetWeapon(weapon.key);
        
        // Check if slot accepts this weapon class
        if (targetSlot.WeaponClass != def.weaponClass)
            return false;
        
        var targetArray = editableLoadout.GetListForClass(def.weaponClass);
        var slots = GetSlotsForClass(def.weaponClass);
        
        // Find the index of the target slot
        int targetIndex = slots.IndexOf(targetSlot);
        if (targetIndex == -1 || targetIndex >= targetArray.Length)
            return false;
        
        // Check if weapon is already in loadout
        int existingIndex = -1;
        for (int i = 0; i < targetArray.Length; i++)
        {
            if (!targetArray[i].Equals(default) && targetArray[i].Compare(weapon))
            {
                existingIndex = i;
                break;
            }
        }
        
        // If weapon is already in loadout, clear its old slot
        if (existingIndex != -1)
        {
            targetArray[existingIndex] = default;
            if (existingIndex < slots.Count)
                slots[existingIndex].ClearSlot();
        }
        
        // If target slot is occupied, clear it
        if (!targetSlot.IsEmpty)
        {
            targetArray[targetIndex] = default;
        }
        
        // Assign weapon to the target slot
        targetArray[targetIndex] = weapon;
        targetSlot.AssignWeapon(weapon);
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
        var targetArray = editableLoadout.GetListForClass(def.weaponClass);
        var slots = GetSlotsForClass(def.weaponClass);
        
        // Find and remove the weapon from the array
        for (int i = 0; i < targetArray.Length; i++)
        {
            if (!targetArray[i].Equals(default) && targetArray[i].Compare(weapon))
            {
                targetArray[i] = default;
                if (i < slots.Count)
                    slots[i].ClearSlot();
                break;
            }
        }
    }

    private List<LoadoutSlot> GetSlotsForClass(WeaponClass wClass)
    {
        return wClass switch
        {
            WeaponClass.Large => largeSlots,
            WeaponClass.Small => smallSlots,
            WeaponClass.Tool => toolSlots,
            _ => null
        };
    }

    private void OnLoadoutConfirmed()
    {
        FirstPersonController.LocalPlayer.GetWeaponManager().SetUpLoadout(editableLoadout);
        CloseMenu();
    }
}
