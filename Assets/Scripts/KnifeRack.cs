using Unity.Netcode;
using UnityEngine;

public class KnifeRack : InteractableBase
{
    public int autoEquipKey = 10;
    public GameObject knifeVisual;

    private NetworkVariable<bool> knifeAvailable = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private FirstPersonController currentHolder;
    private int previousWeaponKey = -1;

    private void Start()
    {
        knifeAvailable.OnValueChanged += (oldValue, newValue) => UpdateKnifeState(newValue);
        UpdateKnifeState(knifeAvailable.Value);
    }

    public override void Interact(FirstPersonController player)
    {
        var wm = player.GetComponent<WeaponManager>();
        if (wm == null)
            return;

        // Case 1: knife is on rack → take it
        if (knifeAvailable.Value)
        {
            if (!player.NetworkObject.IsOwner)
                return;

            // Save their currently equipped weapon before giving knife
            previousWeaponKey = wm.GetEquippedWeaponKey();

            RequestPickupKnifeServerRpc(player.OwnerClientId);
        }
        // Case 2: knife already taken and player is holding it → return it
        else if (wm.GetEquippedWeaponKey() == autoEquipKey)
        {
            // Re-equip old weapon and notify server
            if (previousWeaponKey != -1)
                wm.EquipWeapon(previousWeaponKey);

            NotifyKnifeReturnedServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupKnifeServerRpc(ulong requestingClientId)
    {
        if (!knifeAvailable.Value)
            return;

        knifeAvailable.Value = false;
        PickupKnifeClientRpc(requestingClientId);
    }

    [ClientRpc]
    private void PickupKnifeClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId)
            return;

        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()
            .GetComponent<FirstPersonController>();
        var wm = player.GetComponent<WeaponManager>();

        if (wm == null)
            return;

        wm.EquipWeapon(autoEquipKey);
        wm.OnWeaponChanged += OnWeaponChanged;
        currentHolder = player;
    }

    private void OnWeaponChanged(int? currentWeaponKey)
    {
        if (currentHolder == null)
            return;

        var wm = currentHolder.GetComponent<WeaponManager>();

        // If they unequip the knife without returning it to the rack
        if (currentWeaponKey != autoEquipKey)
        {
            wm.OnWeaponChanged -= OnWeaponChanged;
            currentHolder = null;
            NotifyKnifeReturnedServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyKnifeReturnedServerRpc(ServerRpcParams rpcParams = default)
    {
        knifeAvailable.Value = true;
    }

    private void UpdateKnifeState(bool available)
    {
        if (knifeVisual != null)
            knifeVisual.SetActive(available);
    }

    public override string GetPrompt(FirstPersonController player)
    {
        var wm = player.GetComponent<WeaponManager>();
        if (wm == null)
            return string.Empty;

        if (knifeAvailable.Value)
            return "Press \"E\" to take Knife";
        else if (wm.GetEquippedWeaponKey() == autoEquipKey)
            return "Press \"E\" to return Knife";
        else
            return "No knife available";
    }
}
