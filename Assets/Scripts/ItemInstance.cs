using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

[System.Serializable]
public struct ItemCustomData : INetworkSerializable
{
    // Potion fields
    public int doses;
    public int effectType;
    public float duration;

    // Pelt fields
    public float quality;
    public Color color;
    public FixedString64Bytes description;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref doses);
        serializer.SerializeValue(ref effectType);
        serializer.SerializeValue(ref duration);
        serializer.SerializeValue(ref quality);
        serializer.SerializeValue(ref color);
        serializer.SerializeValue(ref description);
    }

    public bool Equals(ItemCustomData other)
    {
        return doses == other.doses &&
               effectType == other.effectType &&
               Mathf.Approximately(duration, other.duration) &&
               Mathf.Approximately(quality, other.quality) &&
               color.Equals(other.color);
    }

    public override bool Equals(object obj)
    {
        return obj is ItemCustomData other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + doses.GetHashCode();
        hash = hash * 31 + effectType.GetHashCode();
        hash = hash * 31 + duration.GetHashCode();
        hash = hash * 31 + quality.GetHashCode();
        hash = hash * 31 + color.GetHashCode();
        return hash;
    }
}

[System.Serializable]
public struct ItemInstance : INetworkSerializable
{
    public int key;
    public int stackSize;
    public ItemCustomData customData;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref key);
        serializer.SerializeValue(ref stackSize);
        customData.NetworkSerialize(serializer);
    }

    public bool Compare(ItemInstance other)
    {
        if (key != other.key) return false;
        if (!customData.Equals(other.customData)) return false;
        return true;
    }

    // Compare equality (for NGO's internal change tracking)
    public bool Equals(ItemInstance other)
    {
        return key == other.key
            && stackSize == other.stackSize
            && customData.Equals(other.customData);
    }

    // You should also override Equals(object) and GetHashCode for good measure
    public override bool Equals(object obj)
    {
        return obj is ItemInstance other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + key.GetHashCode();
        hash = hash * 31 + stackSize.GetHashCode();
        hash = hash * 31 + customData.GetHashCode();
        return hash;
    }
}