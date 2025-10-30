using UnityEngine;

public class ButcherKnife : Weapon
{
    protected override void HandleFire()
    {
        if (_input.fire)
        {
            InteractableBase currentInteractable = _owner.GetCurrentInteractable();
            // if (currentInteractable is ButcherTable butcherTable)
            // {
            //     butcherTable.ButcherServerRpc(_owner.OwnerClientId);
            // }
            if (currentInteractable is Corpse corpse)
            {
                Debug.Log("Attempting animal butcher");
                var animalReward = corpse.animal.GetComponent<AnimalReward>();
                if (animalReward != null)
                {
                    Debug.Log("Attempting animal butcher 2");
                    animalReward.ButcherServerRpc(_owner.OwnerClientId);
                }
            }
            _input.fire = false;
        }
    }
}
