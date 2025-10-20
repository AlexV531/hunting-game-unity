using Unity.Collections;
using Unity.Netcode;
using System;

public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public ulong ClientId;
    public FixedString64Bytes Name;
    public int Kills;

    // Convenience constructor
    public PlayerData(ulong clientId, FixedString64Bytes name, int kills)
    {
        ClientId = clientId;
        Name = name;
        Kills = kills;
    }

    // INetworkSerializable implementation
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Kills);
    }

    // IEquatable implementation
    public bool Equals(PlayerData other)
    {
        // FixedString64Bytes implements equality
        return ClientId == other.ClientId
            && Name.Equals(other.Name)
            && Kills == other.Kills;
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ClientId.GetHashCode();
            hash = (hash * 397) ^ Name.GetHashCode();
            hash = (hash * 397) ^ Kills;
            return hash;
        }
    }

    public static bool operator ==(PlayerData left, PlayerData right) => left.Equals(right);
    public static bool operator !=(PlayerData left, PlayerData right) => !left.Equals(right);
}
