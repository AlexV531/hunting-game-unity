using UnityEngine;

public class Corpse : InteractableBase
{
    public override void Interact(FirstPersonController player)
    {
        Debug.Log("Interacted with corpse");
    }
}
