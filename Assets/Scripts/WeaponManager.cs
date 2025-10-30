using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class WeaponManager : NetworkBehaviour
{
    [Header("Weapon Container")]
    public Transform weaponContainer;

    private Weapon currentWeapon;
    private Loadout currentLoadout;
    // private ItemInstance previousWeaponInstance = default;
    private ItemInstance equippedWeaponInstance = default;

    // Uses ItemInstance as the key
    private Dictionary<ItemInstance, Weapon> spawnedWeapons = new Dictionary<ItemInstance, Weapon>();
    private Dictionary<int, Weapon> spawnedContextual = new Dictionary<int, Weapon>();
    private List<int> unlockedKeys = new List<int>();
    private PlayerInputs _input;
    private FirstPersonController _player;

    public event System.Action<ItemInstance?> OnWeaponChanged;

    public override void OnNetworkSpawn()
    {
        // equippedWeaponKey.OnValueChanged += OnWeaponChanged;

        if (IsOwner)
        {
            _player = GetComponent<FirstPersonController>();
            _input = GetComponent<PlayerInputs>();

            StartCoroutine(WaitForPlayerAndInit());
        }
    }

    private IEnumerator WaitForPlayerAndInit()
    {
        // Wait until this object is the registered PlayerObject
        while (NetworkManager.Singleton.LocalClient.PlayerObject == null ||
            NetworkManager.Singleton.LocalClient.PlayerObject != NetworkObject)
        {
            yield return null;
        }

        // Safe to initialize weapons now
        PlayerSaveData data = SaveSystem.LoadPlayer();
        if (data != null)
        {
            unlockedKeys = new List<int>(data.unlockedWeaponKeys);

            _player.GetLoadoutManager().InitializePlayerLoadout(data.loadout);
            SetUpLoadout(data.loadout);

            // unlock weapons to be unlocked by default
            // foreach (var def in WeaponDatabase.GetAllWeapons())
            // {
            //     if (def.unlockedByDefault)
            //     {
            //         UnlockWeapon(def.key);
            //     }
            // }

            // Spawn contextual weapons
            foreach (var def in WeaponDatabase.GetAllWeapons())
            {
                if (def.contextual)
                {
                    // if (!spawnedWeapons.ContainsKey(def.key))
                    //     RequestSpawnContextualWeapon(def.key);

                    // DO WE EVEN NEED TO CHECK SPAWNED WEAPONS HERE?
                }
            }

            Debug.Log("Goin to equip the weapon in save file " + data.equippedWeaponInstance.key + " " + data.equippedWeaponInstance.stackSize);
            if (!data.equippedWeaponInstance.Equals(default))
            {
                Debug.Log("Equipping the weapon in save file");
                StartCoroutine(EquipWhenReady(data.equippedWeaponInstance));
            }
        }
    }

    void Update()
    {
        if (!IsOwner || _input == null) return;

        if (_input.equip1) { EquipWeaponInSlot(0); _input.equip1 = false; }
        if (_input.equip2) { EquipWeaponInSlot(1); _input.equip2 = false; }
        if (_input.equip3) { EquipWeaponInSlot(2); _input.equip3 = false; }
        if (_input.equip4) { EquipWeaponInSlot(3); _input.equip4 = false; }
        if (_input.equip5) { EquipWeaponInSlot(4); _input.equip5 = false; }
        if (_input.equip6) { EquipWeaponInSlot(5); _input.equip6 = false; }
        if (_input.equip7) { EquipWeaponInSlot(6); _input.equip7 = false; }
        if (_input.equip8) { EquipWeaponInSlot(7); _input.equip8 = false; }
    }

    public void UnlockWeapon(int key)
    {
        if (WeaponDatabase.GetWeapon(key).contextual)
            return;
        if (!unlockedKeys.Contains(key))
            unlockedKeys.Add(key);
    }

    public void AddToLoadout(ItemInstance weaponInstance)
    {
        if (spawnedWeapons.ContainsKey(weaponInstance)) return;

        RequestSpawnWeapon(weaponInstance);
    }

    public void RemoveFromLoadout(ItemInstance weaponInstance)
    {
        if (!spawnedWeapons.ContainsKey(weaponInstance)) return;

        RequestDespawnWeapon(weaponInstance);
        UnregisterSpawnedWeapon(weaponInstance);
    }

    public void SetUpLoadout(Loadout newLoadout)
    {
        foreach (ItemInstance weaponInstance in spawnedWeapons.Keys)
        {
            RequestDespawnWeapon(weaponInstance);
        }
        foreach (ItemInstance weaponInstance in new List<ItemInstance>(spawnedWeapons.Keys))
        {
            UnregisterSpawnedWeapon(weaponInstance);
        }

        foreach (ItemInstance weaponInstance in newLoadout.largeWeapons)
        {
            RequestSpawnWeapon(weaponInstance);
        }
        foreach (ItemInstance weaponInstance in newLoadout.smallWeapons)
        {
            RequestSpawnWeapon(weaponInstance);
        }
        foreach (ItemInstance weaponInstance in newLoadout.tools)
        {
            RequestSpawnWeapon(weaponInstance);
        }

        currentLoadout = newLoadout;
    }

    private IEnumerator EquipWhenReady(ItemInstance weaponInstance, float timeout = 1f)
    {
        float startTime = Time.time;

        // Wait until the weapon is spawned or timeout occurs
        while (!spawnedWeapons.ContainsKey(weaponInstance))
        {
            if (Time.time - startTime > timeout)
            {
                Debug.LogWarning($"Timeout waiting for weapon {weaponInstance} to spawn");
                yield break; // Exit the coroutine
            }

            yield return null; // Wait one frame
        }

        EquipWeapon(weaponInstance);
    }

    public void EquipWeapon(ItemInstance weaponInstance)
    {
        if (!IsOwner)
        {
            Debug.Log("Attempting to equip a player's weapon without being the player's owner client");
        }

        if (!spawnedWeapons.ContainsKey(weaponInstance) || currentWeapon == spawnedWeapons[weaponInstance]) return;

        // previousWeaponInstance = currentWeapon != null ? currentWeapon.weaponKey : default;

        currentWeapon?.OnUnequip();
        currentWeapon = spawnedWeapons[weaponInstance];
        currentWeapon.OnEquip();

        equippedWeaponInstance = weaponInstance;
        Debug.Log(equippedWeaponInstance.stackSize);

        OnWeaponChanged?.Invoke(equippedWeaponInstance);
    }

    public void EquipWeaponInSlot(int slot)
    {
        if (!IsOwner)
        {
            Debug.Log("Attempting to equip a player's weapon without being the player's owner client");
        }

        if (currentLoadout == null)
        {
            Debug.LogWarning("Loadout not set in weapon manager");
            return;
        }

        ItemInstance key = default;
        if (slot == 0)
        {
            if (currentLoadout.largeWeapons.Count < 1)
            {
                return;
            }
            key = currentLoadout.largeWeapons[0];
        }
        else if (slot == 1)
        {
            if (currentLoadout.largeWeapons.Count < 2)
            {
                return;
            }
            key = currentLoadout.largeWeapons[1];
        }
        else if (slot == 2)
        {
            if (currentLoadout.smallWeapons.Count < 1)
            {
                return;
            }
            key = currentLoadout.smallWeapons[0];
        }
        else if (slot == 3)
        {
            if (currentLoadout.smallWeapons.Count < 2)
            {
                return;
            }
            key = currentLoadout.smallWeapons[1];
        }
        else if (slot == 4)
        {
            if (currentLoadout.tools.Count < 1)
            {
                return;
            }
            key = currentLoadout.tools[0];
        }
        else if (slot == 5)
        {
            if (currentLoadout.tools.Count < 2)
            {
                return;
            }
            key = currentLoadout.tools[1];
        }
        else if (slot == 6)
        {
            if (currentLoadout.tools.Count < 3)
            {
                return;
            }
            key = currentLoadout.tools[2];
        }
        else if (slot == 7)
        {
            if (currentLoadout.tools.Count < 4)
            {
                return;
            }
            key = currentLoadout.tools[3];
        }

        EquipWeapon(key);
    }

    public void RequestSpawnWeapon(ItemInstance weaponInstance)
    {
        if (!IsOwner)
            return;
        if (WeaponDatabase.GetWeapon(weaponInstance.key).contextual)
            return;
        SpawnWeaponServerRpc(weaponInstance);
    }

    public void RequestDespawnWeapon(ItemInstance weaponInstance)
    {
        if (!IsOwner)
            return;
        if (WeaponDatabase.GetWeapon(weaponInstance.key).contextual)
            return;
        DespawnWeaponServerRpc(spawnedWeapons[weaponInstance].NetworkObjectId);
    }

    // public void RequestSpawnContextualWeapon(int key)
    // {
    //     if (!IsOwner)
    //         return;
    //     SpawnWeaponServerRpc(key);
    // }

    // public void RequestDespawnContextualWeapon(ItemInstance key)
    // {
    //     if (!IsOwner)
    //         return;
    //     DespawnWeaponServerRpc(spawnedWeapons[key].NetworkObjectId);
    // }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnWeaponServerRpc(ItemInstance weaponInstance, ServerRpcParams rpcParams = default)
    {
        Debug.Log("Spawning weapon");
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        WeaponDefinition def = WeaponDatabase.GetWeapon(weaponInstance.key);
        if (def == null) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var clientData)) return;
        GameObject playerObj = clientData.PlayerObject.gameObject;

        // Spawn weapon at player's mount position
        Vector3 spawnPos = playerObj.transform.position;
        Quaternion spawnRot = playerObj.transform.rotation;

        GameObject obj = Instantiate(def.prefab, spawnPos, spawnRot);
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Weapon prefab must have a NetworkObject!");
            Destroy(obj);
            return;
        }

        // Give ownership to the client
        netObj.SpawnWithOwnership(senderClientId, true);
        netObj.GetComponent<Weapon>().weaponInstance.Value = weaponInstance;

        // IF WEAPON HAS ANY PROPERTIES UNIQUE TO IT'S WEAPON INSTANCE THEY NEED TO BE CHANGED HERE
    }

    public void RegisterSpawnedWeapon(ItemInstance weaponInstance, Weapon weapon)
    {
        Debug.Log("Weapon registered");
        spawnedWeapons[weaponInstance] = weapon;
    }

    public void UnregisterSpawnedWeapon(ItemInstance weaponInstance)
    {
        if (WeaponDatabase.GetWeapon(weaponInstance.key).contextual)
            return;
        Debug.Log("Weapon unregistered");
        spawnedWeapons.Remove(weaponInstance);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnWeaponServerRpc(ulong weaponNetworkId, ServerRpcParams rpcParams = default)
    {
        // Validate that this weapon actually exists
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(weaponNetworkId, out var weapon))
        {
            Debug.LogWarning($"Weapon with ID {weaponNetworkId} not found on server");
            return;
        }

        // Security check: make sure the caller actually owns this weapon
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (weapon.OwnerClientId != senderClientId)
        {
            Debug.LogWarning($"Client {senderClientId} tried to despawn weapon not owned by them.");
            return;
        }

        // Now safely despawn
        weapon.Despawn(true);
    }

    public Weapon GetCurrentWeapon() => currentWeapon;

    public List<int> GetUnlockedWeaponKeys() => unlockedKeys;

    public ItemInstance GetEquippedWeaponInstance() => equippedWeaponInstance;
}
