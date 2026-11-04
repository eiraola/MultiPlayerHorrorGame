using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private static readonly TimeSpan CountdownStep = TimeSpan.FromSeconds(1);

        [SerializeField] private StringValueBus playerNameBus;
        [SerializeField] private StringValueBus roomNameBus;
        [SerializeField] private StringValueBus joinCodeBus;

        [Tooltip("Players in arrival order, so the host is always the first one.")]
        [SerializeField] private LobbyPlayerEntryArrayValueBus playersBus;

        [SerializeField] private BoolValueBus localReadyBus;
        [SerializeField] private BoolValueBus isHostBus;
        [SerializeField] private BoolValueBus canStartBus;
        [SerializeField] private IntValueBus countdownBus;
        [SerializeField] private EventBus startRequestBus;

        private readonly NetworkVariable<FixedString128Bytes> roomName = new();
        private readonly NetworkVariable<FixedString32Bytes> joinCode = new();
        private readonly NetworkVariable<int> countdown = new();
        private NetworkList<LobbyPlayer> players;
        private CancellationTokenSource countdownCts;

        // NetworkList must be created before spawning, and not in the field initializer, to avoid a native leak.
        private void Awake() => players = new NetworkList<LobbyPlayer>();

        private void OnEnable()
        {
            localReadyBus.Changed += HandleLocalReadyChanged;
            startRequestBus.Raised += HandleStartRequested;
        }

        private void OnDisable()
        {
            localReadyBus.Changed -= HandleLocalReadyChanged;
            startRequestBus.Raised -= HandleStartRequested;
        }

        public override void OnNetworkSpawn()
        {
            roomName.OnValueChanged += HandleRoomNameChanged;
            joinCode.OnValueChanged += HandleJoinCodeChanged;
            countdown.OnValueChanged += HandleCountdownChanged;
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

            // Reliable RPCs arrive in order, so the host already knows this player when this one lands.
            if (localReadyBus.Value)
                SetReadyRpc(true);

            PublishAll();
        }

        public override void OnNetworkDespawn()
        {
            roomName.OnValueChanged -= HandleRoomNameChanged;
            joinCode.OnValueChanged -= HandleJoinCodeChanged;
            countdown.OnValueChanged -= HandleCountdownChanged;
            players.OnListChanged -= HandlePlayersChanged;

            StopCountdown();

            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        public override void OnDestroy()
        {
            // The scene is destroyed when leaving the room or starting the game, and both need clean data afterwards.
            roomNameBus.Set(null);
            joinCodeBus.Set(null);
            playersBus.Set(Array.Empty<LobbyPlayerEntry>());
            localReadyBus.Set(false);
            isHostBus.Set(false);
            canStartBus.Set(false);
            countdownBus.Set(LobbyRules.NoCountdown);

            base.OnDestroy();
        }

        [Rpc(SendTo.Server)]
        private void RegisterPlayerRpc(FixedString128Bytes playerName, RpcParams rpcParams = default)
        {
            // The client's text is not trusted: it gets the same cleanup as the local field.
            var cleanName = ToFixed<FixedString128Bytes>(PlayerName.Normalize(playerName.ToString()));
            AddPlayer(rpcParams.Receive.SenderClientId, cleanName);
        }

        [Rpc(SendTo.Server)]
        private void SetReadyRpc(bool isReady, RpcParams rpcParams = default)
        {
            if (countdown.Value == LobbyRules.CountdownFinished)
                return;

            int index = IndexOf(rpcParams.Receive.SenderClientId);

            if (index < 0 || players[index].IsReady == isReady)
                return;

            LobbyPlayer player = players[index];
            player.IsReady = isReady;
            players[index] = player;

            if (!isReady)
                CancelCountdown();
        }

        private void AddPlayer(ulong clientId, FixedString128Bytes playerName)
        {
            if (IndexOf(clientId) >= 0)
                return;

            players.Add(new LobbyPlayer(clientId, playerName));
            CancelCountdown();
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            int index = IndexOf(clientId);

            if (index < 0)
                return;

            players.RemoveAt(index);
            CancelCountdown();
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

        private void HandleLocalReadyChanged(bool isReady)
        {
            // Before spawning there is nobody to tell: OnNetworkSpawn sends the value instead.
            if (IsSpawned)
                SetReadyRpc(isReady);
        }

        private void HandleStartRequested()
        {
            if (!IsSpawned || !IsServer)
                return;

            if (LobbyRules.CanPressStart(BuildEntries(), countdown.Value) && IsEveryoneRegistered())
                StartCountdown();
        }

        private void StartCountdown()
        {
            countdownCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            CountdownAsync(countdownCts.Token).Forget();
        }

        private void StopCountdown()
        {
            countdownCts?.Cancel();
            countdownCts?.Dispose();
            countdownCts = null;
        }

        private void CancelCountdown()
        {
            if (countdown.Value == LobbyRules.CountdownFinished)
                return;

            StopCountdown();
            countdown.Value = LobbyRules.NoCountdown;
        }

        private async UniTaskVoid CountdownAsync(CancellationToken ct)
        {
            for (int remaining = LobbyRules.CountdownStart; remaining > 0; remaining--)
            {
                countdown.Value = remaining;

                if (await UniTask.Delay(CountdownStep, ignoreTimeScale: true, cancellationToken: ct).SuppressCancellationThrow())
                    return;

                if (!IsEveryoneRegistered())
                {
                    CancelCountdown();
                    return;
                }
            }

            StopCountdown();
            countdown.Value = LobbyRules.CountdownFinished;
        }

        // A client still synchronizing is connected but not listed yet, so the list alone cannot tell.
        private bool IsEveryoneRegistered() =>
            LobbyRules.IsEveryoneRegistered(players.Count, NetworkManager.ConnectedClientsIds.Count);

        private void HandleRoomNameChanged(FixedString128Bytes _, FixedString128Bytes value) => roomNameBus.Set(value.ToString());

        private void HandleJoinCodeChanged(FixedString32Bytes _, FixedString32Bytes value) => joinCodeBus.Set(value.ToString());

        private void HandleCountdownChanged(int _, int value)
        {
            countdownBus.Set(value);
            PublishCanStart();
        }

        private void HandlePlayersChanged(NetworkListEvent<LobbyPlayer> _) => PublishPlayers();

        private void PublishAll()
        {
            HandleRoomNameChanged(default, roomName.Value);
            HandleJoinCodeChanged(default, joinCode.Value);
            isHostBus.Set(IsServer);
            countdownBus.Set(countdown.Value);
            PublishPlayers();
        }

        private void PublishPlayers()
        {
            playersBus.Set(BuildEntries());
            PublishCanStart();
        }

        // Without the connection count: inside the disconnect callback it can still include the one leaving.
        private void PublishCanStart() => canStartBus.Set(IsServer && LobbyRules.CanPressStart(playersBus.Value, countdown.Value));

        private LobbyPlayerEntry[] BuildEntries()
        {
            var entries = new LobbyPlayerEntry[players.Count];

            for (int i = 0; i < entries.Length; i++)
                entries[i] = new LobbyPlayerEntry(players[i].Name.ToString(), players[i].IsReady);

            return entries;
        }

        private static T ToFixed<T>(string text) where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var fixedText = new T();
            fixedText.CopyFromTruncated(text ?? string.Empty);
            return fixedText;
        }
    }
}
