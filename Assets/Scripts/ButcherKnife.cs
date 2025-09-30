using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using Unity.Netcode;

public class ButcherKnife : Weapon
{
    protected FirstPersonController _player;

    protected override void Start()
    {
        base.Start();
        _player = GetComponentInParent<FirstPersonController>();
    }

    protected override void Update()
    {
        HandleAim();
        HandleFire();
    }

    protected override void HandleFire()
    {
        if (_input.fire)
        {
            InteractableBase currentInteractable = _player.GetCurrentInteractable();
            if (currentInteractable is ButcherTable butcherTable)
            {
                butcherTable.ButcherServerRpc(_player.OwnerClientId);
            }
            _input.fire = false;
        }
    }
}
