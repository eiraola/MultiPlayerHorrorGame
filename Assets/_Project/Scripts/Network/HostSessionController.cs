using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ProjectScare.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectScare.Network
{
    public sealed class HostSessionController : MonoBehaviour
    {
        private const string TimeoutMessage = "Creating the room took too long. Please try again.";
        private const string GenericFailureMessage = "The room could not be created. Please try again.";
        private const string BusyMessage = "Another game is being set up. Please try again in a moment.";

        // Relay counts the connections the host accepts, not the host itself. The lobby counts everyone.
        private const int MaxRemotePlayers = 1;
        private const int MaxLobbyPlayers = MaxRemotePlayers + 1;

        private static readonly TimeSpan CreationTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan RegionQueryTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport transport;
        [SerializeField] private SessionServices sessionServices;
        [SerializeField] private HostGameRequestBus hostRequestedBus;
        [SerializeField] private StringEventBus hostFailedBus;
        [SerializeField] private EventBus contentUnloadRequestBus;
        [SerializeField] private StringArrayValueBus regionsBus;
        [SerializeField] private StringValueBus joinCodeBus;
        [SerializeField] private StringValueBus roomNameBus;
        [SerializeField] private EventBus sessionEndedBus;

        private Task<Lobby> lobbyCreation;
        private CancellationTokenSource heartbeatCts;

        private void Awake()
        {
            lobbyCreation = null;
            heartbeatCts = null;
        }

        private void OnEnable()
        {
            hostRequestedBus.Raised += HandleHostRequested;
            sessionEndedBus.Raised += HandleSessionEnded;
        }

        private void OnDisable()
        {
            hostRequestedBus.Raised -= HandleHostRequested;
            sessionEndedBus.Raised -= HandleSessionEnded;
        }

        private void Start() => LoadRegionsAsync(destroyCancellationToken).Forget();

        private void OnDestroy()
        {
            StopHeartbeat();

            // Best effort: the process may die first, and then the lobby expires without its heartbeat.
            RemoveLobbyAsync(lobbyCreation).Forget();
        }

        private async UniTaskVoid LoadRegionsAsync(CancellationToken destroyToken)
        {
            using var queryCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            using IDisposable timeout = queryCts.CancelAfterSlim(RegionQueryTimeout, DelayType.UnscaledDeltaTime);

            try
            {
                await sessionServices.EnsureSignedInAsync(queryCts.Token);

                List<Region> regions = await RelayService.Instance
                    .ListRegionsAsync()
                    .AsUniTask()
                    .AttachExternalCancellation(queryCts.Token);

                regionsBus.Set(regions.ConvertAll(region => region.Id).ToArray());
            }
            catch (Exception e)
            {
                // Without the list the window still offers Automatic, so creating a room stays possible.
                if (!destroyToken.IsCancellationRequested)
                    Debug.LogError($"Listing the regions failed: {e}", this);
            }
        }

        private void HandleHostRequested(HostGameRequest request)
        {
            if (!sessionServices.TryBeginOperation())
            {
                Debug.LogWarning($"Host request '{request.Name}' refused: another operation is running.", this);
                hostFailedBus.Raise(BusyMessage);
                return;
            }

            CreateRoomAsync(request, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid CreateRoomAsync(HostGameRequest request, CancellationToken destroyToken)
        {
            using var creationCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            using IDisposable timeout = creationCts.CancelAfterSlim(CreationTimeout, DelayType.UnscaledDeltaTime);

            try
            {
                await sessionServices.EnsureSignedInAsync(creationCts.Token);

                string relayCode = await CreateRelayAllocationAsync(request.Region, creationCts.Token);
                Lobby lobby = await CreateLobbyAsync(request, relayCode, creationCts.Token);

                StartHeartbeat(lobby.Id, destroyToken);
                joinCodeBus.Set(lobby.LobbyCode);
                roomNameBus.Set(lobby.Name);

                StartHost();
                await LoadLobbySceneAsync(creationCts.Token);
            }
            catch (Exception e)
            {
                // Netcode shuts itself down in its own OnDestroy, and nobody is left to read the failure.
                if (!destroyToken.IsCancellationRequested)
                    await HandleCreationFailureAsync(e, destroyToken);

                return;
            }

            Debug.Log($"Room '{request.Name}' created.", this);
            sessionServices.EndOperation();
            contentUnloadRequestBus.Raise();
        }

        // A null region lets the service pick the closest one.
        private async UniTask<string> CreateRelayAllocationAsync(string region, CancellationToken ct)
        {
            Allocation allocation = await RelayService.Instance
                .CreateAllocationAsync(MaxRemotePlayers, region)
                .AsUniTask()
                .AttachExternalCancellation(ct);

            string relayCode = await RelayService.Instance
                .GetJoinCodeAsync(allocation.AllocationId)
                .AsUniTask()
                .AttachExternalCancellation(ct);

            transport.SetRelayServerData(allocation.ToRelayServerData(SessionServices.RelayConnectionType));

            return relayCode;
        }

        private async UniTask<Lobby> CreateLobbyAsync(HostGameRequest request, string relayCode, CancellationToken ct)
        {
            var options = new CreateLobbyOptions
            {
                IsPrivate = !request.IsPublic,
                Password = request.Password,

                // Only members receive it, so the Relay allocation cannot be reached without passing the password.
                Data = new Dictionary<string, DataObject>
                {
                    [SessionServices.RelayCodeKey] = new DataObject(DataObject.VisibilityOptions.Member, relayCode),
                },
            };

            // Kept because the request cannot be cancelled: after a timeout it may still create a lobby to remove.
            lobbyCreation = LobbyService.Instance.CreateLobbyAsync(request.Name, MaxLobbyPlayers, options);

            return await lobbyCreation.AsUniTask().AttachExternalCancellation(ct);
        }

        private void StartHeartbeat(string lobbyId, CancellationToken destroyToken)
        {
            heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            HeartbeatLoopAsync(lobbyId, heartbeatCts.Token).Forget();
        }

        private void StopHeartbeat()
        {
            heartbeatCts?.Cancel();
            heartbeatCts?.Dispose();
            heartbeatCts = null;
        }

        private async UniTaskVoid HeartbeatLoopAsync(string lobbyId, CancellationToken ct)
        {
            while (!await UniTask.Delay(HeartbeatInterval, ignoreTimeScale: true, cancellationToken: ct).SuppressCancellationThrow())
            {
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                }
                catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
                {
                    Debug.LogWarning("The lobby no longer exists, so its heartbeat stops.", this);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"A lobby heartbeat failed: {e}", this);
                }
            }
        }

        private void StartHost()
        {
            if (!networkManager.StartHost())
                throw new InvalidOperationException("Netcode could not start as host.");

            NetworkSceneManager sceneManager = networkManager.SceneManager;

            // The filter goes first because the synchronization mode reads it to pick the scenes it keeps out of the list.
            sceneManager.VerifySceneBeforeLoading = (_, sceneName, _) => sceneName == SessionServices.LobbySceneName;
            sceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
        }

        private async UniTask LoadLobbySceneAsync(CancellationToken ct)
        {
            var loadCompleted = new UniTaskCompletionSource();

            // Kept in a local because Netcode clears its own property when it shuts down.
            NetworkSceneManager sceneManager = networkManager.SceneManager;

            void HandleLoadCompleted(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
            {
                if (sceneName == SessionServices.LobbySceneName)
                    loadCompleted.TrySetResult();
            }

            sceneManager.OnLoadEventCompleted += HandleLoadCompleted;

            try
            {
                // Additive because Single would also unload the persistent Bootstrap scene.
                SceneEventProgressStatus status = sceneManager.LoadScene(SessionServices.LobbySceneName, LoadSceneMode.Additive);

                if (status != SceneEventProgressStatus.Started)
                    throw new InvalidOperationException($"Loading '{SessionServices.LobbySceneName}' was refused: {status}.");

                await loadCompleted.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                sceneManager.OnLoadEventCompleted -= HandleLoadCompleted;
            }

            // Netcode only changes the active scene on Single loads.
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SessionServices.LobbySceneName));
        }

        private async UniTask HandleCreationFailureAsync(Exception failure, CancellationToken destroyToken)
        {
            Debug.LogError($"Room creation failed: {SessionServices.DescribeFailure(failure)}", this);

            string playerMessage = failure is OperationCanceledException ? TimeoutMessage : GenericFailureMessage;

            StopHeartbeat();

            try
            {
                await sessionServices.ShutdownNetcodeAsync(destroyToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogError($"Aborting the host failed: {e}", this);
            }

            // Cleared before waiting so OnDestroy does not remove the same lobby twice.
            Task<Lobby> abandonedCreation = lobbyCreation;
            lobbyCreation = null;

            await sessionServices.WaitForLobbyCleanupAsync(RemoveLobbyAsync(abandonedCreation), destroyToken);

            if (destroyToken.IsCancellationRequested)
                return;

            sessionServices.EndOperation();
            joinCodeBus.Set(null);
            roomNameBus.Set(null);
            hostFailedBus.Raise(playerMessage);
        }

        // Removing the lobby makes its code invalid at once, instead of waiting for it to expire.
        private void HandleSessionEnded()
        {
            StopHeartbeat();

            Task<Lobby> endedCreation = lobbyCreation;
            lobbyCreation = null;
            RemoveLobbyAsync(endedCreation).Forget();
        }

        private async UniTask RemoveLobbyAsync(Task<Lobby> creation)
        {
            if (creation == null || creation.IsFaulted)
                return;

            try
            {
                Lobby lobby = await creation;
                await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Removing the lobby failed: {SessionServices.DescribeFailure(e)}", this);
            }
        }
    }
}
