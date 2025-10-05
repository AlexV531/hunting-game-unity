using UnityEngine;

public class ButcherKnife : Weapon
{
    protected override void HandleFire()
    {
        if (_input.fire)
        {
            InteractableBase currentInteractable = _owner.GetCurrentInteractable();
            if (currentInteractable is ButcherTable butcherTable)
            {
                butcherTable.ButcherServerRpc(_owner.OwnerClientId);
            }
            _input.fire = false;
        }
    }
}
