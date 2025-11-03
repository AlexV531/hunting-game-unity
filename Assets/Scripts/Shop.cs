using System.Collections.Generic;
public class Shop : InteractableBase
{
    public List<ItemInstance> purchasableItemInstances;

    public override void Interact(FirstPersonController player)
    {
        player.GetShopUI().OpenShopMenu(this);
    }
}