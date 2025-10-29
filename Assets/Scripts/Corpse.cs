using UnityEngine;

public class Corpse : InteractableBase
{
    public Animal animal;

    public override void Interact(FirstPersonController player)
    {
        if (player.IsShoulderCarrying.Value)
        {
            return;
        }
        player.PickUpAnimalServerRpc(animal.NetworkObject);
        SetInteractionEnabledServerRpc(false);
    }

    public override string GetPrompt(FirstPersonController player)
    {
        return "Press \"e\" to pick up animal \n Press \"t\" to inspect";
    }
}
