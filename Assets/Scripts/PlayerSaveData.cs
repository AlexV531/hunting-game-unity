using System.Collections.Generic;

[System.Serializable]
public class PlayerSaveData
{
    public int money;
    public List<int> unlockedWeaponKeys = new List<int>();
    public Inventory inventory = new Inventory();
    public Inventory storageInventory = new Inventory();
    public Loadout loadout = new Loadout();
    public ItemInstance equippedWeaponInstance = default;
}