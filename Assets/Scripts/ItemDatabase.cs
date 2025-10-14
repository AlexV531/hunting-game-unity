using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeReference]
    public List<ItemDefinition> allItems = new List<ItemDefinition>();

    private Dictionary<int, ItemDefinition> lookup;

    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (_instance == null)
                {
                    Debug.LogError("ItemDatabase not found in Resources!");
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
        lookup = new Dictionary<int, ItemDefinition>();

        foreach (var item in allItems)
        {
            if (item == null)
                continue;

            if (lookup.ContainsKey(item.key))
            {
                Debug.LogWarning($"Duplicate item key {item.key} for {item.itemName}");
                continue;
            }

            lookup.Add(item.key, item);
        }
    }

    public ItemDefinition GetItem(int key)
    {
        if (lookup == null) Initialize();
        lookup.TryGetValue(key, out var item);
        return item;
    }

    // Helper to get all items of a specific subclass type (e.g., WeaponDefinition)
    public IEnumerable<T> GetItemsOfType<T>() where T : ItemDefinition
    {
        if (lookup == null) Initialize();

        foreach (var item in lookup.Values)
        {
            if (item is T typedItem)
                yield return typedItem;
        }
    }
}
