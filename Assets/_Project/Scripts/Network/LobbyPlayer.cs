using System;
using Unity.Collections;
using Unity.Netcode;

namespace ProjectScare.Network
{
    public struct LobbyPlayer : INetworkSerializable, IEquatable<LobbyPlayer>
    {
        public ulong ClientId;
        public FixedString128Bytes Name;
        public bool IsReady;

        public LobbyPlayer(ulong clientId, FixedString128Bytes name)
        {
            ClientId = clientId;
            Name = name;
            IsReady = false;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref IsReady);
        }

        // IsReady takes part because the list only notices a rewritten entry if it compares as different.
        public bool Equals(LobbyPlayer other) => ClientId == other.ClientId && Name.Equals(other.Name) && IsReady == other.IsReady;
    }
}
