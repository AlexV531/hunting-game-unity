using Unity.Netcode;
using Unity.Collections;

public struct HitDataStrings : INetworkSerializable
{
    public FixedString512Bytes string1;
    public FixedString512Bytes string2;
    public FixedString512Bytes string3;

    // Serialization logic
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref string1);
        serializer.SerializeValue(ref string2);
        serializer.SerializeValue(ref string3);
    }
}