using System.Collections.Generic;
using UnityEngine;

public static class WeaponDatabase
{
    public static WeaponDefinition GetWeapon(int key)
    {
        return ItemDatabase.Instance.GetItem(key) as WeaponDefinition;
    }

    public static IEnumerable<WeaponDefinition> GetAllWeapons()
    {
        return ItemDatabase.Instance.GetItemsOfType<WeaponDefinition>();
    }
}