using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Shop : InteractableBase
{
    public List<int> purchasableItemKeys;

    public override void Interact(FirstPersonController player)
    {
        player.GetShopUI().OpenShopMenu(this);
    }
}