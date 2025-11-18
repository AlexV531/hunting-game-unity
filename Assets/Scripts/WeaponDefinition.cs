using UnityEngine;

[System.Serializable]
public class WeaponDefinition : ItemDefinition
{
    public GameObject prefab; // prefab to instantiate
    public bool unlockedByDefault;
    public bool contextual;

    public override string ToString()
    {
        return $"WeaponDefinition [Key={key}, Name={itemName}, Price={price}, " +
            $"UnlockedByDefault={unlockedByDefault}, Contextual={contextual}, " +
            $"Prefab={(prefab != null ? prefab.name : "None")}, Icon={(icon != null ? icon.name : "None")}]";
    }
}