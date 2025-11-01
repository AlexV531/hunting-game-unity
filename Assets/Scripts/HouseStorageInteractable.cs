using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HouseStorageInteractable : InteractableBase
{
    public override void Interact(FirstPersonController player)
    {
        player.GetStorageMenu().OpenStorageMenu(player.GetInventory(), player.GetStorageInventory());
    }
}