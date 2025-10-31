using UnityEngine;

public enum ItemType
{
    None,
    Potion,
    AnimalPelt,
    Weapon,
    Consumable
}

[System.Serializable]
public class ItemDefinition
{
    public int key;
    public string itemName;
    public Sprite icon;
    public int price;
    public ItemSize itemSize;
    public ItemType itemType;
    public bool stackable;
    public GameObject worldAppearancePrefab;

    public override string ToString()
    {
        return $"ItemDefinition [Key={key}, Name={itemName}, Price={price}, Icon={(icon != null ? icon.name : "None")}]";
    }
}
