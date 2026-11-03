using System;
using Unity.Collections;
using Unity.Netcode;

namespace ProjectScare.Network
{
    public struct LobbyPlayer : INetworkSerializable, IEquatable<LobbyPlayer>
    {
        public ulong ClientId;
        public FixedString128Bytes Name;

        public LobbyPlayer(ulong clientId, FixedString128Bytes name)
        {
            ClientId = clientId;
            Name = name;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
        }

        public bool Equals(LobbyPlayer other) => ClientId == other.ClientId && Name.Equals(other.Name);
    }
}
