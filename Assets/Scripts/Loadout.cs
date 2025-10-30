using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Loadout
{
    public List<ItemInstance> largeWeapons = new List<ItemInstance>(2);
    public List<ItemInstance> smallWeapons = new List<ItemInstance>(2);
    public List<ItemInstance> tools = new List<ItemInstance>(4);

    public override string ToString()
    {
        string ListToString(string label, List<ItemInstance> list)
        {
            if (list == null || list.Count == 0)
                return $"{label}: None";

            var entries = list.Select((w, i) =>
                !w.Equals(default)
                    ? $"[{i}] (Key {w.key})"
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