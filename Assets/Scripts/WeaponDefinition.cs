using UnityEngine;

[System.Serializable]
public class WeaponDefinition
{
    public int weaponKey;         // unique identifier
    public string weaponName;     // display name
    public GameObject prefab;     // prefab to instantiate
    public bool unlockedByDefault;
    public bool contextual;
    public Sprite icon;
    public WeaponClass weaponClass;
}