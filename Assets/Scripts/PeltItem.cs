using UnityEngine;

[System.Serializable]
public class PeltItem : ItemInstance
{
    public Color furColor;
    public float quality;

    public override bool Compare(ItemInstance other)
    {
        // PeltItem otherPelt = (PeltItem)other;
        // if (otherPelt == null)
        //     return false;

        // return key == other.key && furColor == otherPelt.furColor && quality == otherPelt.quality;

        // Nonstackable, so always false
        return false;
    }
}