using System.Collections.Generic;

[System.Serializable]
public class PlayerSaveData
{
    public int money;
    public List<int> unlockedWeaponKeys = new List<int>();
    public Loadout loadout = new Loadout();
    public int equippedWeaponKey = -1; // -1 means nothing equipped
}