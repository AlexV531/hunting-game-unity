using UnityEngine;

[System.Serializable]
public class WeaponDefinition : ItemDefinition
{
    public GameObject prefab; // prefab to instantiate
    public bool unlockedByDefault;
    public bool contextual;
    public WeaponClass weaponClass;

    public override void Acquire(FirstPersonController player)
    {
        player.GetWeaponManager().UnlockWeapon(key);
    }
}