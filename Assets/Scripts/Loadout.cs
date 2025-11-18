using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Loadout
{
    public ItemInstance[] largeWeapons = new ItemInstance[2];
    public ItemInstance[] smallWeapons = new ItemInstance[2];
    public ItemInstance[] tools = new ItemInstance[4];

    public ItemInstance[] GetListForClass(WeaponClass wClass) => wClass switch
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
            largeWeapons = (ItemInstance[])largeWeapons.Clone(),
            smallWeapons = (ItemInstance[])smallWeapons.Clone(),
            tools = (ItemInstance[])tools.Clone()
        };
    }

    public List<ItemInstance> GetAllWeapons()
    {
        var all = new List<ItemInstance>();
        all.AddRange(largeWeapons.Where(w => !w.Equals(default)));
        all.AddRange(smallWeapons.Where(w => !w.Equals(default)));
        all.AddRange(tools.Where(w => !w.Equals(default)));
        return all;
    }

    public ItemInstance GetWeaponInSlot(int slot)
    {
        return slot switch
        {
            0 => largeWeapons[0],
            1 => largeWeapons[1],
            2 => smallWeapons[0],
            3 => smallWeapons[1],
            4 => tools[0],
            5 => tools[1],
            6 => tools[2],
            7 => tools[3],
            _ => default
        };
    }

    public bool SetWeaponInSlot(int slot, ItemInstance weapon)
    {
        switch (slot)
        {
            case 0: largeWeapons[0] = weapon; return true;
            case 1: largeWeapons[1] = weapon; return true;
            case 2: smallWeapons[0] = weapon; return true;
            case 3: smallWeapons[1] = weapon; return true;
            case 4: tools[0] = weapon; return true;
            case 5: tools[1] = weapon; return true;
            case 6: tools[2] = weapon; return true;
            case 7: tools[3] = weapon; return true;
            default: return false;
        }
    }

    public bool HasKey(int key)
    {
        return largeWeapons.Any(w => !w.Equals(default) && w.key == key)
            || smallWeapons.Any(w => !w.Equals(default) && w.key == key)
            || tools.Any(w => !w.Equals(default) && w.key == key);
    }

    public int GetFirstEmptySlot(WeaponClass wClass)
    {
        var arr = GetListForClass(wClass);
        if (arr == null) return -1;
        
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i].Equals(default)) return i;
        }
        return -1;
    }

    public override string ToString()
    {
        string Large = string.Join(", ", largeWeapons.Select(w => w.Equals(default) ? "Empty" : w.key.ToString()));
        string Small = string.Join(", ", smallWeapons.Select(w => w.Equals(default) ? "Empty" : w.key.ToString()));
        string Tool = string.Join(", ", tools.Select(w => w.Equals(default) ? "Empty" : w.key.ToString()));
        return $"Loadout:\nLarge: [{Large}]\nSmall: [{Small}]\nTools: [{Tool}]";
    }
}
