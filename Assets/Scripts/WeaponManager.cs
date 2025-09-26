using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class WeaponManager : NetworkBehaviour
{
    public Transform weaponContainer;
    public int defaultWeaponKey = 0;

    private Dictionary<int, Weapon> weapons = new Dictionary<int, Weapon>();
    private Weapon currentWeapon;

    // NetworkVariable to sync equipped weapon key
    private NetworkVariable<int> equippedWeaponKey = new NetworkVariable<int>(
        -1, // -1 = no weapon equipped initially
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private PlayerInputs _input;

    void Start()
    {
        _input = GetComponent<PlayerInputs>();
        // Populate dictionary with all weapons in the container
        Weapon[] weaponArray = weaponContainer.GetComponentsInChildren<Weapon>(true);
        foreach (var w in weaponArray)
        {
            if (!weapons.ContainsKey(w.weaponKey))
                weapons.Add(w.weaponKey, w);
            w.OnUnequip(); // start inactive
        }

        equippedWeaponKey.OnValueChanged += OnWeaponChanged;

        // Apply the current value immediately for late-joining clients
        if (equippedWeaponKey.Value != -1)
        {
            OnWeaponChanged(-1, equippedWeaponKey.Value);
        }
        else if (IsOwner && weapons.ContainsKey(defaultWeaponKey))
        {
            // If no weapon is equipped yet, equip default
            EquipWeapon(defaultWeaponKey);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (_input.equip1)
        {
            EquipWeapon(0);
            _input.equip1 = false;
        }
        if (_input.equip2)
        {
            EquipWeapon(1);
            _input.equip2 = false;
        }
        if (_input.equip3)
        {
            EquipWeapon(2);
            _input.equip3 = false;
        }
    }

    public void EquipWeapon(int key)
    {
        if (!weapons.ContainsKey(key)) return;
        if (currentWeapon != null && currentWeapon.weaponKey == key) return;

        // Unequip current
        if (currentWeapon != null)
            currentWeapon.OnUnequip();

        // Equip new weapon
        currentWeapon = weapons[key];
        currentWeapon.OnEquip();

        // Sync the equipped weapon key across the network
        if (IsOwner)
            equippedWeaponKey.Value = key;
    }

    public Weapon GetCurrentWeapon()
    {
        return currentWeapon;
    }

    private void OnWeaponChanged(int previous, int current)
    {
        if (IsOwner) return; // owner already applied locally
        if (!weapons.ContainsKey(current)) return;

        EquipWeapon(current);
    }
}
