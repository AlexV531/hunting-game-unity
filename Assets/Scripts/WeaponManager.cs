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

    // private NetworkVariable<int> equippedWeaponKey = new NetworkVariable<int>(
    //     -1,
    //     NetworkVariableReadPermission.Everyone,
    //     NetworkVariableWritePermission.Owner
    // );
    private int equippedWeaponKey = -1;

    private Dictionary<int, Weapon> spawnedWeapons = new Dictionary<int, Weapon>();
    private List<int> unlockedKeys = new List<int>();
    private PlayerInputs _input;
    // private FirstPersonController _ownerController;

    public override void OnNetworkSpawn()
    {
        // equippedWeaponKey.OnValueChanged += OnWeaponChanged;

        if (IsOwner)
        {
            // _ownerController = GetComponent<FirstPersonController>();
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

            foreach (int key in data.loadoutWeaponKeys)
            {
                Debug.Log("Requesting to spawn a weapon");
                RequestSpawnWeapon(key, false);
            }

            foreach (var def in WeaponDatabase.Instance.allWeapons)
            {
                if (def.unlockedByDefault)
                {
                    UnlockWeapon(def.weaponKey);
                    if (!spawnedWeapons.ContainsKey(def.weaponKey))
                        RequestSpawnWeapon(def.weaponKey, false);
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
        if (_input.equip3) { EquipWeapon(2); _input.equip3 = false; }
    }

    public void UnlockWeapon(int key)
    {
        if (!unlockedKeys.Contains(key))
            unlockedKeys.Add(key);
    }

    public void AddToLoadout(int key)
    {
        if (!unlockedKeys.Contains(key) || spawnedWeapons.ContainsKey(key)) return;

        RequestSpawnWeapon(key, false);
    }

    public void RemoveFromLoadout(int key)
    {
        if (!spawnedWeapons.ContainsKey(key)) return;

        // DespawnWeapon(key);
    }

    private void OnWeaponChanged(int previous, int current)
    {
        if (IsOwner)
            return;
        Debug.Log("Swapping to weapon " + current);
        if (current != -1)
        {
            EquipWeapon(current);
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

        // Debug.Log("Equipping weapon key " + key);
        // foreach(KeyValuePair<int, Weapon> entry in spawnedWeapons)
        // {
        //     Debug.Log("key " + entry.Key + " value " + entry.Value.name);
        // }
        if (!spawnedWeapons.ContainsKey(key) || currentWeapon == spawnedWeapons[key]) return;
        // Debug.Log("Made it 1");

        previousWeaponKey = currentWeapon != null ? currentWeapon.weaponKey : -1;

        currentWeapon?.OnUnequip();
        currentWeapon = spawnedWeapons[key];
        currentWeapon.OnEquip();

        equippedWeaponKey = key;

        // if (IsOwner)
        // {
        //     equippedWeaponKey.Value = key;
        //     Debug.Log("Made it 2");
        // }
    }

    // private void DespawnWeapon(int key)
    // {
    //     if (!spawnedWeapons.TryGetValue(key, out Weapon weapon)) return;

    //     NetworkObject netObj = weapon.GetComponent<NetworkObject>();
    //     if (netObj != null && netObj.IsSpawned)
    //     {
    //         if (IsServer)
    //         {
    //             netObj.Despawn(true);
    //         }
    //         else
    //         {
    //             DespawnWeaponServerRpc(key);
    //         }
    //     }

    //     spawnedWeapons.Remove(key);
    //     Destroy(weapon.gameObject);
    // }

    public void RequestSpawnWeapon(int key, bool autoEquip = false)
    {
        if (!IsOwner) return;
        SpawnWeaponServerRpc(key, autoEquip);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnWeaponServerRpc(int key, bool autoEquip, ServerRpcParams rpcParams = default)
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

    // [ServerRpc(RequireOwnership = false)]
    // private void DespawnWeaponServerRpc(int key)
    // {
    //     DespawnWeapon(key);
    // }

    public Weapon GetCurrentWeapon() => currentWeapon;

    public List<int> GetUnlockedWeaponKeys() => unlockedKeys;

    public int GetEquippedWeaponKey() => equippedWeaponKey;

    // public int GetEquippedWeaponKey() => equippedWeaponKey.Value;
}
