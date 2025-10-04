using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponDefinition> allWeapons;

    private Dictionary<int, WeaponDefinition> lookup;

    // Singleton instance
    private static WeaponDatabase _instance;
    public static WeaponDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // Load from Resources folder
                _instance = Resources.Load<WeaponDatabase>("WeaponDatabase");
                if (_instance == null)
                {
                    Debug.LogError("WeaponDatabase not found in Resources!");
                }
                else
                {
                    _instance.Initialize();
                }
            }
            return _instance;
        }
    }

    public void Initialize()
    {
        if (lookup != null) return; // already initialized
        lookup = new Dictionary<int, WeaponDefinition>();
        foreach (var def in allWeapons)
        {
            if (!lookup.ContainsKey(def.weaponKey))
                lookup.Add(def.weaponKey, def);
        }
    }

    public WeaponDefinition GetWeapon(int key)
    {
        if (lookup == null) Initialize();
        return lookup.ContainsKey(key) ? lookup[key] : null;
    }
}
