using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class LoadoutManager : MonoBehaviour
{
    [Header("UI References (assign in Inspector)")]
    public GameObject loadoutScreen;
    public Transform weaponListContainer;
    public GameObject weaponButtonPrefab;
    public Button confirmButton;

    [Header("Slot Containers")]
    public List<LoadoutSlot> largeSlots;
    public List<LoadoutSlot> smallSlots;
    public List<LoadoutSlot> toolSlots;

    private Loadout currentLoadout = new Loadout();
    private bool loadoutOpen = true;

    private void Start()
    {
        // Initialize each slot with its type
        foreach (var s in largeSlots) s.Initialize(WeaponClass.Large, this);
        foreach (var s in smallSlots) s.Initialize(WeaponClass.Small, this);
        foreach (var s in toolSlots) s.Initialize(WeaponClass.Tool, this);

        confirmButton.onClick.AddListener(OnLoadoutConfirmed);

        CloseLoadoutScreen();
        CursorManager.SetCursorActive(true);
    }

    // This should be called when the player opens the loadout screen
    public void OpenLoadoutScreen()
    {
        loadoutScreen.SetActive(true);
        RefreshWeaponList();
        CursorManager.SetCursorActive(true);
        loadoutOpen = true;
    }

    public void CloseLoadoutScreen()
    {
        loadoutScreen.SetActive(false);
        CursorManager.SetCursorActive(false);
        loadoutOpen = false;
    }

    public bool IsLoadoutOpen()
    {
        return loadoutOpen;
    }

    private void RefreshWeaponList()
    {
        if (FirstPersonController.LocalPlayer == null)
            return;

        // Clear old buttons
        foreach (Transform child in weaponListContainer)
        {
            Destroy(child.gameObject);
        }

        // Rebuild from current weapon list
        foreach (ItemInstance weaponItem in FirstPersonController.LocalPlayer.GetInventory().GetWeapons())
        {
            if (WeaponDatabase.GetWeapon(weaponItem.key).contextual)
                continue;
            GameObject btnObj = Instantiate(weaponButtonPrefab, weaponListContainer);
            WeaponButton btn = btnObj.GetComponent<WeaponButton>();
            btn.Initialize(weaponItem, this);

            // Change this to incorporate elements of the specific instance of the weapon as well
        }
    }

    public void InitializePlayerLoadout(Loadout initialLoadout)
    {
        foreach (LoadoutSlot slot in largeSlots)
        {
            slot.ClearSlot();
        }
        foreach (LoadoutSlot slot in smallSlots)
        {
            slot.ClearSlot();
        }
        foreach (LoadoutSlot slot in toolSlots)
        {
            slot.ClearSlot();
        }
        foreach (ItemInstance weaponInstance in initialLoadout.largeWeapons)
        {
            OnWeaponSelected(weaponInstance);
        }
        foreach (ItemInstance weaponInstance in initialLoadout.smallWeapons)
        {
            OnWeaponSelected(weaponInstance);
        }
        foreach (ItemInstance weaponInstance in initialLoadout.tools)
        {
            OnWeaponSelected(weaponInstance);
        }
    }

    public void OnWeaponSelected(ItemInstance weapon)
    {
        WeaponDefinition weaponDef = WeaponDatabase.GetWeapon(weapon.key);

        var targetList = GetListForClass(weaponDef.weaponClass);
        foreach (ItemInstance item in targetList)
        {
            Debug.Log(item.key);
        }
        Debug.Log(targetList);
        int maxSlots = targetList.Capacity;
        Debug.Log(targetList.Count + " | " + maxSlots);


        if (targetList.Count >= maxSlots)
        {
            Debug.Log($"No free slot for {weaponDef.weaponClass} (likely list capacities in Loadout object are messed up)");
            return;
        }

        // Find a free slot in the appropriate group
        var slot = GetFreeSlotForClass(weaponDef.weaponClass);
        if (slot == null)
        {
            Debug.Log("All slots are filled for " + weaponDef.weaponClass);
            return;
        }

        slot.AssignWeapon(weapon);
        targetList.Add(weapon);
    }

    private List<ItemInstance> GetListForClass(WeaponClass wClass)
    {
        return wClass switch
        {
            WeaponClass.Large => currentLoadout.largeWeapons,
            WeaponClass.Small => currentLoadout.smallWeapons,
            WeaponClass.Tool => currentLoadout.tools,
            _ => default
        };
    }

    private LoadoutSlot GetFreeSlotForClass(WeaponClass wClass)
    {
        List<LoadoutSlot> slots = wClass switch
        {
            WeaponClass.Large => largeSlots,
            WeaponClass.Small => smallSlots,
            WeaponClass.Tool => toolSlots,
            _ => null
        };

        if (slots == null) return null;

        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
                return slot;
        }

        return null;
    }

    public Loadout GetCurrentLoadout()
    {
        return currentLoadout;
    }

    public void RemoveWeaponFromLoadout(ItemInstance weapon)
    {
        WeaponDefinition weaponDef = WeaponDatabase.GetWeapon(weapon.key);

        var list = GetListForClass(weaponDef.weaponClass);
        list.Remove(weapon);
        Debug.Log($"Removed {weaponDef.itemName} from {weaponDef.weaponClass} loadout");
    }

    private void OnLoadoutConfirmed()
    {
        CloseLoadoutScreen();

        if (FirstPersonController.LocalPlayer != null)
        {
            FirstPersonController.LocalPlayer.GetWeaponManager().SetUpLoadout(currentLoadout);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && IsLoadoutOpen())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
