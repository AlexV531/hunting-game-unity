using System.Collections.Generic;
public class BalloonShop : Shop
{
    void Start()
    {
        ItemInstance balloons = new ItemInstance()
        {
            key = 5,
            stackSize = 5
        };
        purchasableItemInstances.Add(balloons);
    }
}