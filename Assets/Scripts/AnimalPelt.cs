using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

[System.Serializable]
public struct AnimalPelt : INetworkSerializable
{
    public Color Color;
    public FixedString64Bytes Description;

    public AnimalPelt(Color color, string description)
    {
        Color = color;
        Description = new FixedString64Bytes(description);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Color);
        serializer.SerializeValue(ref Description);
    }
}