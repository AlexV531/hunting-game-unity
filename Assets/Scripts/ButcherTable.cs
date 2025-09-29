using UnityEngine;
using Unity.Netcode;

public class ButcheringTable : AnimalStoringInteractableBase
{
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
}