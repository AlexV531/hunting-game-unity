using UnityEngine;

[System.Serializable]
public class ItemDefinition
{
    public int key; // unique identifier
    public string itemName; // display name
    public Sprite icon;
    public int price;
    public ItemSize itemSize;
    public bool stackable;

    public override string ToString()
    {
        return $"ItemDefinition [Key={key}, Name={itemName}, Price={price}, Icon={(icon != null ? icon.name : "None")}]";
    }
}
