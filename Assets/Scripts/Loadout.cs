using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Loadout
{
    public List<WeaponDefinition> largeWeapons = new List<WeaponDefinition>(2);
    public List<WeaponDefinition> smallWeapons = new List<WeaponDefinition>(2);
    public List<WeaponDefinition> tools = new List<WeaponDefinition>(4);

    public override string ToString()
    {
        string ListToString(string label, List<WeaponDefinition> list)
        {
            if (list == null || list.Count == 0)
                return $"{label}: None";

            var entries = list.Select((w, i) =>
                w != null
                    ? $"[{i}] {w.itemName} (Key {w.key})"
                    : $"[{i}] null");

            return $"{label}: {string.Join(", ", entries)}";
        }

        return
            $"Loadout:\n" +
            $"  {ListToString("Large Weapons", largeWeapons)}\n" +
            $"  {ListToString("Small Weapons", smallWeapons)}\n" +
            $"  {ListToString("Tools", tools)}";
    }
}