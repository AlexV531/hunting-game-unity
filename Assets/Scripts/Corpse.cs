using UnityEngine;

public class Corpse : InteractableBase
{
    public Animal animal;

    public override void Interact(FirstPersonController player)
    {
        player.PickUpAnimal(animal);
    }
}
