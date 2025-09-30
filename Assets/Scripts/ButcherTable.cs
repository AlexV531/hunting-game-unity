using UnityEngine;
using Unity.Netcode;

public class ButcherTable : AnimalStoringInteractableBase
{
    public float tableRange;
    public int autoEquipKey = 10;

    public override void Interact(FirstPersonController player)
    {
        if (player.IsCarryingAnimal.Value && GetPlacedAnimal() == null)
        {
            player.PlaceAnimalServerRpc(NetworkObject);
        }
        else if (GetPlacedAnimal() != null)
        {
            player.PickUpAnimalServerRpc(GetPlacedAnimal().NetworkObject);
            ClearPlacedAnimal();
        }
    }

    public override string GetPrompt(FirstPersonController player)
    {
        if (player.IsCarryingAnimal.Value)
        {
            return "Press \"e\" to place animal";
        }
        else if (GetPlacedAnimal() != null)
        {
            return "Press \"e\" to pick up animal";
        }
        else
        {
            return "No animal to place";
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ButcherServerRpc(ulong clientId)
    {
        Debug.Log("Animal butchered");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            var wm = other.GetComponent<WeaponManager>();
            var netObj = other.GetComponent<NetworkObject>();
            if (wm != null && netObj != null)
            {
                wm.AutoEquipWeaponClientRpc(autoEquipKey, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { netObj.OwnerClientId }
                    }
                });
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            var wm = other.GetComponent<WeaponManager>();
            var netObj = other.GetComponent<NetworkObject>();
            if (wm != null && netObj != null)
            {
                wm.AutoUnequipWeaponClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { netObj.OwnerClientId }
                    }
                });
            }
        }
    }
}