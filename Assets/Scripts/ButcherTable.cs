using UnityEngine;
using Unity.Netcode;

public class ButcherTable : AttachInteractable
{
    public float tableRange;
    public int autoEquipKey = 10;

    public override void Interact(FirstPersonController player)
    {
        if (player.GetCarriedAnimal() != null)
        {
            Animal animalToAttach = player.GetCarriedAnimal();
            if (animalToAttach != null)
            {
                player.DropAnimalServerRpc();
                AttachAnimalServerRpc(animalToAttach.NetworkObject);
            }
            else
                Debug.Log("player.GetCarriedAnimal() failed");
            return;
        }
    }

    public override string GetPrompt(FirstPersonController player)
    {
        if (player.GetCarriedAnimal() != null)
        {
            return "Press \"e\" to place animal";
        }
        else
        {
            return "No animal to place";
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AttachAnimalServerRpc(NetworkObjectReference animalRef)
    {
        if (!animalRef.TryGet(out NetworkObject netObj)) return;
		if (!netObj.TryGetComponent<Animal>(out var animalToAttach)) return;

        BalloonAttach animalAttach = animalToAttach.GetComponent<BalloonAttach>();
        if (animalAttach != null)
        {
            AttachTarget(animalAttach);
            var animalReward = animalToAttach.GetComponent<AnimalReward>();
            if (animalReward != null)
            {
                animalReward.butcherable = true;
            }
        }
        else
            Debug.Log("animalToAttach.GetComponent<BalloonAttach>() failed");
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (!IsServer) return;

    //     if (other.CompareTag("Player"))
    //     {
    //         var wm = other.GetComponent<WeaponManager>();
    //         if (wm != null && other.TryGetComponent<FirstPersonController>(out var player))
    //         {
    //             wm.EquipWeapon(autoEquipKey); // NEEDS CLIENT RPC
    //         }
    //     }
    // }

    // private void OnTriggerExit(Collider other)
    // {
    //     if (!IsServer) return;

    //     if (other.CompareTag("Player"))
    //     {
    //         var wm = other.GetComponent<WeaponManager>();
    //         if (wm != null)
    //         {
    //             // wm.AutoUnequipWeapon();
    //         }
    //     }
    // }
}