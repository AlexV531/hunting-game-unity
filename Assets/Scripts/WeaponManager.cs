using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class WeaponManager : NetworkBehaviour
{
    [Header("Weapon Container")]
    public Transform weaponContainer;

    private Weapon currentWeapon;
    private int previousWeaponKey = -1;
    private int equippedWeaponKey = -1;

    private Dictionary<int, Weapon> spawnedWeapons = new Dictionary<int, Weapon>();
    private Dictionary<int, Weapon> defaultSpawnedWeapons = new Dictionary<int, Weapon>();
    private List<int> unlockedKeys = new List<int>();
    private PlayerInputs _input;
    private FirstPersonController _player;

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
            foreach (var def in WeaponDatabase.Instance.allWeapons)
            {
                if (def.unlockedByDefault)
                {
                    UnlockWeapon(def.weaponKey);
                }
            }

            // Spawn contextual weapons
            foreach (var def in WeaponDatabase.Instance.allWeapons)
            {
                if (def.contextual)
                {
                    if (!spawnedWeapons.ContainsKey(def.weaponKey))
                        RequestSpawnContextualWeapon(def.weaponKey);
                }
            }

            Debug.Log("Goin to equip the weapon in save file " + data.equippedWeaponKey);
            if (data.equippedWeaponKey != -1)
            {
                Debug.Log("Equipping the weapon in save file");
                StartCoroutine(EquipWhenReady(data.equippedWeaponKey));
            }
        }
    }

    void Update()
    {
        if (!IsOwner || _input == null) return;

        if (_input.equip1) { EquipWeapon(0); _input.equip1 = false; }
        if (_input.equip2) { EquipWeapon(10); _input.equip2 = false; }
        if (_input.equip3) { EquipWeapon(5); _input.equip3 = false; }
    }

    public void UnlockWeapon(int key)
    {
        if (WeaponDatabase.Instance.GetWeapon(key).contextual)
            return;
        if (!unlockedKeys.Contains(key))
            unlockedKeys.Add(key);
    }

    public void AddToLoadout(int key)
    {
        if (!unlockedKeys.Contains(key) || spawnedWeapons.ContainsKey(key)) return;

        RequestSpawnWeapon(key);
    }

    public void RemoveFromLoadout(int key)
    {
        if (!spawnedWeapons.ContainsKey(key)) return;

        RequestDespawnWeapon(key);
        UnregisterSpawnedWeapon(key);
    }

    public void SetUpLoadout(Loadout newLoadout)
    {
        foreach (int weaponKey in spawnedWeapons.Keys)
        {
            RequestDespawnWeapon(weaponKey);
        }
        foreach (int weaponKey in new List<int>(spawnedWeapons.Keys))
        {
            UnregisterSpawnedWeapon(weaponKey);
        }

        foreach (WeaponDefinition weaponDef in newLoadout.largeWeapons)
        {
            RequestSpawnWeapon(weaponDef.weaponKey);
        }
        foreach (WeaponDefinition weaponDef in newLoadout.smallWeapons)
        {
            RequestSpawnWeapon(weaponDef.weaponKey);
        }
        foreach (WeaponDefinition weaponDef in newLoadout.tools)
        {
            RequestSpawnWeapon(weaponDef.weaponKey);
        }
    }

    private IEnumerator EquipWhenReady(int key, float timeout = 1f)
    {
        float startTime = Time.time;

        // Wait until the weapon is spawned or timeout occurs
        while (!spawnedWeapons.ContainsKey(key))
        {
            if (Time.time - startTime > timeout)
            {
                Debug.LogWarning($"Timeout waiting for weapon {key} to spawn");
                yield break; // Exit the coroutine
            }

            yield return null; // Wait one frame
        }

        EquipWeapon(key);
    }

    public void EquipWeapon(int key)
    {
        if (!IsOwner)
        {
            Debug.Log("Attempting to equip a player's weapon without being the player's owner client, use autoequip if you are the server");
        }

        if (!spawnedWeapons.ContainsKey(key) || currentWeapon == spawnedWeapons[key]) return;

        previousWeaponKey = currentWeapon != null ? currentWeapon.weaponKey : -1;

        currentWeapon?.OnUnequip();
        currentWeapon = spawnedWeapons[key];
        currentWeapon.OnEquip();

        equippedWeaponKey = key;
    }

    public void RequestSpawnWeapon(int key)
    {
        if (!IsOwner)
            return;
        if (WeaponDatabase.Instance.GetWeapon(key).contextual)
            return;
        SpawnWeaponServerRpc(key);
    }

    public void RequestDespawnWeapon(int key)
    {
        if (!IsOwner)
            return;
        if (WeaponDatabase.Instance.GetWeapon(key).contextual)
            return;
        DespawnWeaponServerRpc(spawnedWeapons[key].NetworkObjectId);
    }

    public void RequestSpawnContextualWeapon(int key)
    {
        if (!IsOwner)
            return;
        SpawnWeaponServerRpc(key);
    }

    public void RequestDespawnContextualWeapon(int key)
    {
        if (!IsOwner)
            return;
        DespawnWeaponServerRpc(spawnedWeapons[key].NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnWeaponServerRpc(int key, ServerRpcParams rpcParams = default)
    {
        Debug.Log("Spawning weapon");
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        WeaponDefinition def = WeaponDatabase.Instance.GetWeapon(key);
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
    }

    public void RegisterSpawnedWeapon(int key, Weapon weapon)
    {
        Debug.Log("Weapon registered");
        spawnedWeapons[key] = weapon;
    }

    public void UnregisterSpawnedWeapon(int key)
    {
        if (WeaponDatabase.Instance.GetWeapon(key).contextual)
            return;
        Debug.Log("Weapon unregistered");
        spawnedWeapons.Remove(key);
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

    public int GetEquippedWeaponKey() => equippedWeaponKey;
}
