using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    [SerializeReference]
    public List<ItemInstance> items = new List<ItemInstance>();
}