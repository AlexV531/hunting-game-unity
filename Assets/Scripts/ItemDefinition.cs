using UnityEngine;

[System.Serializable]
public class ItemDefinition
{
    public int key; // unique identifier
    public string itemName; // display name
    public Sprite icon;
    public int price;

    public virtual void Acquire(FirstPersonController player)
    {
        Debug.Log(player.name + " acquired item " + itemName);
    }
}