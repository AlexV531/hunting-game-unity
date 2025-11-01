using System.Collections.Generic;
public class Shop : InteractableBase
{
    public List<int> purchasableItemKeys;

    public override void Interact(FirstPersonController player)
    {
        player.GetShopUI().OpenShopMenu(this);
    }
}