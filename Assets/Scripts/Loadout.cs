using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Loadout
{
    public List<ItemInstance> largeWeapons = new();
    public List<ItemInstance> smallWeapons = new();
    public List<ItemInstance> tools = new();

    public List<ItemInstance> GetListForClass(WeaponClass wClass) => wClass switch
    {
        WeaponClass.Large => largeWeapons,
        WeaponClass.Small => smallWeapons,
        WeaponClass.Tool => tools,
        _ => null
    };

    public int GetMaxCountForClass(WeaponClass wClass) => wClass switch
    {
        WeaponClass.Large => 2,
        WeaponClass.Small => 2,
        WeaponClass.Tool => 4,
        _ => 0
    };

    public Loadout Clone()
    {
        return new Loadout
        {
            largeWeapons = new List<ItemInstance>(largeWeapons),
            smallWeapons = new List<ItemInstance>(smallWeapons),
            tools = new List<ItemInstance>(tools)
        };
    }

    public List<ItemInstance> GetAllWeapons()
    {
        var all = new List<ItemInstance>();
        all.AddRange(largeWeapons);
        all.AddRange(smallWeapons);
        all.AddRange(tools);
        return all;
    }

    public ItemInstance? GetWeaponInSlot(int slot)
    {
        // Example mapping of slots to classes
        return slot switch
        {
            0 => largeWeapons.ElementAtOrDefault(0),
            1 => largeWeapons.ElementAtOrDefault(1),
            2 => smallWeapons.ElementAtOrDefault(0),
            3 => smallWeapons.ElementAtOrDefault(1),
            4 => tools.ElementAtOrDefault(0),
            5 => tools.ElementAtOrDefault(1),
            6 => tools.ElementAtOrDefault(2),
            7 => tools.ElementAtOrDefault(3),
            _ => null
        };
    }

    public bool HasKey(int key)
    {
        return largeWeapons.Any(w => w.key == key)
            || smallWeapons.Any(w => w.key == key)
            || tools.Any(w => w.key == key);
    }

    public override string ToString()
    {
        string Large = string.Join(", ", largeWeapons.Select(w => w.key.ToString()));
        string Small = string.Join(", ", smallWeapons.Select(w => w.key.ToString()));
        string Tool = string.Join(", ", tools.Select(w => w.key.ToString()));

        return $"Loadout:\nLarge: [{Large}]\nSmall: [{Small}]\nTools: [{Tool}]";
    }
}
