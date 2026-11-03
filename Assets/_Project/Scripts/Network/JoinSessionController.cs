using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ProjectScare.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace ProjectScare.Network
{
    public sealed class JoinSessionController : MonoBehaviour
    {
        private const string TimeoutMessage = "Joining the room took too long. Please try again.";
        private const string GenericFailureMessage = "Could not join the room. Please try again.";
        private const string BusyMessage = "Another game is being set up. Please try again in a moment.";
        private const string WrongPasswordMessage = "The password is incorrect or required.";
        private const string InvalidRequestMessage = "Please check the code and the password.";
        private const string RoomNotFoundMessage = "The code is not valid or the room no longer exists.";
        private const string RoomFullMessage = "The room is full.";

        private static readonly TimeSpan JoinTimeout = TimeSpan.FromSeconds(20);

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport transport;
        [SerializeField] private SessionServices sessionServices;
        [SerializeField] private JoinGameRequestBus joinRequestedBus;
        [SerializeField] private StringEventBus joinFailedBus;
        [SerializeField] private EventBus contentUnloadRequestBus;

        private Task<Lobby> lobbyJoin;

        private void Awake() => lobbyJoin = null;

        private void OnEnable() => joinRequestedBus.Raised += HandleJoinRequested;

        private void OnDisable() => joinRequestedBus.Raised -= HandleJoinRequested;

        // Best effort: if the process dies first, the seat stays taken until the host creates another room.
        private void OnDestroy() => LeaveLobbyAsync(lobbyJoin).Forget();

        private void HandleJoinRequested(JoinGameRequest request)
        {
            if (!sessionServices.TryBeginOperation())
            {
                Debug.LogWarning("Join request refused: another operation is running.", this);
                joinFailedBus.Raise(BusyMessage);
                return;
            }

            JoinRoomAsync(request, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid JoinRoomAsync(JoinGameRequest request, CancellationToken destroyToken)
        {
            using var joinCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            using IDisposable timeout = joinCts.CancelAfterSlim(JoinTimeout, DelayType.UnscaledDeltaTime);

            try
            {
                await sessionServices.EnsureSignedInAsync(joinCts.Token);

                Lobby lobby = await JoinLobbyAsync(request, joinCts.Token);
                await JoinRelayAllocationAsync(lobby.Data[SessionServices.RelayCodeKey].Value, joinCts.Token);
                await StartClientAndSynchronizeAsync(joinCts.Token);
            }
            catch (Exception e)
            {
                // Netcode shuts itself down in its own OnDestroy, and nobody is left to read the failure.
                if (!destroyToken.IsCancellationRequested)
                    await HandleJoinFailureAsync(e, destroyToken);

                return;
            }

            Debug.Log($"Joined room '{request.Code}'.", this);
            sessionServices.EndOperation();
            contentUnloadRequestBus.Raise();
        }

        private async UniTask<Lobby> JoinLobbyAsync(JoinGameRequest request, CancellationToken ct)
        {
            var options = new JoinLobbyByCodeOptions { Password = request.Password };

            // Kept because the request cannot be cancelled: after a timeout it may still take a seat to give back.
            lobbyJoin = LobbyService.Instance.JoinLobbyByCodeAsync(request.Code, options);

            return await lobbyJoin.AsUniTask().AttachExternalCancellation(ct);
        }

        private async UniTask JoinRelayAllocationAsync(string relayCode, CancellationToken ct)
        {
            JoinAllocation allocation = await RelayService.Instance
                .JoinAllocationAsync(relayCode)
                .AsUniTask()
                .AttachExternalCancellation(ct);

            transport.SetRelayServerData(allocation.ToRelayServerData(SessionServices.RelayConnectionType));
        }

        private async UniTask StartClientAndSynchronizeAsync(CancellationToken ct)
        {
            var synchronized = new UniTaskCompletionSource();
            NetworkSceneManager sceneManager = null;

            void HandleSynchronizeComplete(ulong _) => synchronized.TrySetResult();

            void HandleDisconnected(ulong _) =>
                synchronized.TrySetException(new InvalidOperationException("Disconnected from the host while joining."));

            void HandleTransportFailure() =>
                synchronized.TrySetException(new InvalidOperationException("The transport failed while joining."));

            networkManager.OnClientDisconnectCallback += HandleDisconnected;
            networkManager.OnTransportFailure += HandleTransportFailure;

            try
            {
                if (!networkManager.StartClient())
                    throw new InvalidOperationException("Netcode could not start as client.");

                // Kept in a local because Netcode clears its own property when it shuts down.
                sceneManager = networkManager.SceneManager;
                sceneManager.OnSynchronizeComplete += HandleSynchronizeComplete;

                await synchronized.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                networkManager.OnClientDisconnectCallback -= HandleDisconnected;
                networkManager.OnTransportFailure -= HandleTransportFailure;

                if (sceneManager != null)
                    sceneManager.OnSynchronizeComplete -= HandleSynchronizeComplete;
            }
        }

        private async UniTask HandleJoinFailureAsync(Exception failure, CancellationToken destroyToken)
        {
            Debug.LogError($"Joining the room failed: {SessionServices.DescribeFailure(failure)}", this);

            string playerMessage = ToPlayerMessage(failure);

            try
            {
                await sessionServices.ShutdownNetcodeAsync(destroyToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogError($"Aborting the client failed: {e}", this);
            }

            // Cleared before waiting so OnDestroy does not leave the same lobby twice.
            Task<Lobby> abandonedJoin = lobbyJoin;
            lobbyJoin = null;

            await sessionServices.WaitForLobbyCleanupAsync(LeaveLobbyAsync(abandonedJoin), destroyToken);

            if (destroyToken.IsCancellationRequested)
                return;

            sessionServices.EndOperation();
            joinFailedBus.Raise(playerMessage);
        }

        private async UniTask LeaveLobbyAsync(Task<Lobby> join)
        {
            if (join == null || join.IsFaulted)
                return;

            try
            {
                Lobby lobby = await join;
                await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Leaving the lobby failed: {SessionServices.DescribeFailure(e)}", this);
            }
        }

        private static string ToPlayerMessage(Exception failure)
        {
            if (failure is OperationCanceledException)
                return TimeoutMessage;

            if (failure is not LobbyServiceException lobbyFailure)
                return GenericFailureMessage;

            return lobbyFailure.Reason switch
            {
                LobbyExceptionReason.IncorrectPassword => WrongPasswordMessage,
                LobbyExceptionReason.ValidationError => InvalidRequestMessage,
                LobbyExceptionReason.InvalidJoinCode => RoomNotFoundMessage,
                LobbyExceptionReason.LobbyNotFound => RoomNotFoundMessage,
                LobbyExceptionReason.LobbyFull => RoomFullMessage,
                _ => GenericFailureMessage,
            };
        }
    }
}
