using ProjectScare.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ProjectScare.Network
{
    /// <summary>
    /// Room data shared with every player while in MultiplayerLobby. The host writes it and
    /// each instance copies it into its local buses, which are the only thing the UI reads.
    /// </summary>
    public sealed class LobbyState : NetworkBehaviour
    {
        [SerializeField] private StringValueBus playerNameBus;
        [SerializeField] private StringValueBus roomNameBus;
        [SerializeField] private StringValueBus joinCodeBus;

        [Tooltip("Player names in arrival order, so the host is always the first one.")]
        [SerializeField] private StringArrayValueBus playersBus;

        private readonly NetworkVariable<FixedString128Bytes> roomName = new();
        private readonly NetworkVariable<FixedString32Bytes> joinCode = new();
        private NetworkList<LobbyPlayer> players;

        // NetworkList must be created before spawning, and not in the field initializer, to avoid a native leak.
        private void Awake() => players = new NetworkList<LobbyPlayer>();

        public override void OnNetworkSpawn()
        {
            roomName.OnValueChanged += HandleRoomNameChanged;
            joinCode.OnValueChanged += HandleJoinCodeChanged;
            players.OnListChanged += HandlePlayersChanged;

            if (IsServer)
            {
                roomName.Value = ToFixed<FixedString128Bytes>(roomNameBus.Value);
                joinCode.Value = ToFixed<FixedString32Bytes>(joinCodeBus.Value);
                AddPlayer(NetworkManager.LocalClientId, ToFixed<FixedString128Bytes>(playerNameBus.Value));
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }
            else
            {
                RegisterPlayerRpc(ToFixed<FixedString128Bytes>(playerNameBus.Value));
            }

            PublishAll();
        }

        public override void OnNetworkDespawn()
        {
            roomName.OnValueChanged -= HandleRoomNameChanged;
            joinCode.OnValueChanged -= HandleJoinCodeChanged;
            players.OnListChanged -= HandlePlayersChanged;

            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        public override void OnDestroy()
        {
            // The scene is only destroyed when leaving the room, so the next one starts without old data.
            roomNameBus.Set(null);
            joinCodeBus.Set(null);
            playersBus.Set(System.Array.Empty<string>());

            base.OnDestroy();
        }

        [Rpc(SendTo.Server)]
        private void RegisterPlayerRpc(FixedString128Bytes playerName, RpcParams rpcParams = default)
        {
            // The client's text is not trusted: it gets the same cleanup as the local field.
            var cleanName = ToFixed<FixedString128Bytes>(PlayerName.Normalize(playerName.ToString()));
            AddPlayer(rpcParams.Receive.SenderClientId, cleanName);
        }

        private void AddPlayer(ulong clientId, FixedString128Bytes playerName)
        {
            if (IndexOf(clientId) < 0)
                players.Add(new LobbyPlayer(clientId, playerName));
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            int index = IndexOf(clientId);

            if (index >= 0)
                players.RemoveAt(index);
        }

        private int IndexOf(ulong clientId)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].ClientId == clientId)
                    return i;
            }

            return -1;
        }

        private void HandleRoomNameChanged(FixedString128Bytes _, FixedString128Bytes value) => roomNameBus.Set(value.ToString());

        private void HandleJoinCodeChanged(FixedString32Bytes _, FixedString32Bytes value) => joinCodeBus.Set(value.ToString());

        private void HandlePlayersChanged(NetworkListEvent<LobbyPlayer> _) => PublishPlayers();

        private void PublishAll()
        {
            HandleRoomNameChanged(default, roomName.Value);
            HandleJoinCodeChanged(default, joinCode.Value);
            PublishPlayers();
        }

        private void PublishPlayers()
        {
            var names = new string[players.Count];

            for (int i = 0; i < names.Length; i++)
                names[i] = players[i].Name.ToString();

            playersBus.Set(names);
        }

        private static T ToFixed<T>(string text) where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var fixedText = new T();
            fixedText.CopyFromTruncated(text ?? string.Empty);
            return fixedText;
        }
    }
}
