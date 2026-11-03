using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ProjectScare.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectScare.Network
{
    public sealed class HostSessionController : MonoBehaviour
    {
        private const string LobbySceneName = "MultiplayerLobby";
        private const string RelayConnectionType = "dtls";
        private const string TimeoutMessage = "Creating the room took too long. Please try again.";
        private const string GenericFailureMessage = "The room could not be created. Please try again.";

        // Relay counts the connections the host accepts, not the host itself.
        private const int MaxRemotePlayers = 1;

        private static readonly TimeSpan CreationTimeout = TimeSpan.FromSeconds(20);

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport transport;
        [SerializeField] private HostGameRequestBus hostRequestedBus;
        [SerializeField] private StringEventBus hostFailedBus;
        [SerializeField] private EventBus contentUnloadRequestBus;

        private bool isCreating;
        private Task signInTask;

        private void Awake()
        {
            isCreating = false;
            signInTask = null;
        }

        private void OnEnable() => hostRequestedBus.Raised += HandleHostRequested;

        private void OnDisable() => hostRequestedBus.Raised -= HandleHostRequested;

        private void HandleHostRequested(HostGameRequest request)
        {
            if (isCreating)
            {
                Debug.LogWarning($"Host request '{request.Name}' ignored: a room is already being created.", this);
                return;
            }

            CreateRoomAsync(request, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid CreateRoomAsync(HostGameRequest request, CancellationToken destroyToken)
        {
            isCreating = true;

            using var creationCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            using IDisposable timeout = creationCts.CancelAfterSlim(CreationTimeout, DelayType.UnscaledDeltaTime);

            try
            {
                await StartRelayHostAsync(creationCts.Token);
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
            isCreating = false;
            contentUnloadRequestBus.Raise();
        }

        private async UniTask StartRelayHostAsync(CancellationToken ct)
        {
            // A second anonymous sign-in is rejected while another is running, so a retry joins the abandoned one.
            if (signInTask == null || signInTask.IsCompleted)
                signInTask = InitializeAndSignInAsync();

            await signInTask.AsUniTask().AttachExternalCancellation(ct);

            Allocation allocation = await RelayService.Instance
                .CreateAllocationAsync(MaxRemotePlayers)
                .AsUniTask()
                .AttachExternalCancellation(ct);

            transport.SetRelayServerData(allocation.ToRelayServerData(RelayConnectionType));

            if (!networkManager.StartHost())
                throw new InvalidOperationException("Netcode could not start as host.");
        }

        private static async Task InitializeAndSignInAsync()
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        private async UniTask LoadLobbySceneAsync(CancellationToken ct)
        {
            var loadCompleted = new UniTaskCompletionSource();

            // Kept in a local because Netcode clears its own property when it shuts down.
            NetworkSceneManager sceneManager = networkManager.SceneManager;

            void HandleLoadCompleted(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
            {
                if (sceneName == LobbySceneName)
                    loadCompleted.TrySetResult();
            }

            sceneManager.OnLoadEventCompleted += HandleLoadCompleted;

            try
            {
                // Additive because Single would also unload the persistent Bootstrap scene.
                SceneEventProgressStatus status = sceneManager.LoadScene(LobbySceneName, LoadSceneMode.Additive);

                if (status != SceneEventProgressStatus.Started)
                    throw new InvalidOperationException($"Loading '{LobbySceneName}' was refused: {status}.");

                await loadCompleted.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                sceneManager.OnLoadEventCompleted -= HandleLoadCompleted;
            }

            // Netcode only changes the active scene on Single loads.
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(LobbySceneName));
        }

        private async UniTask HandleCreationFailureAsync(Exception failure, CancellationToken destroyToken)
        {
            Debug.LogError($"Room creation failed: {failure}", this);

            string playerMessage = failure is OperationCanceledException ? TimeoutMessage : GenericFailureMessage;

            try
            {
                await AbortHostAsync(destroyToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogError($"Aborting the host failed: {e}", this);
            }

            if (destroyToken.IsCancellationRequested)
                return;

            isCreating = false;
            hostFailedBus.Raise(playerMessage);
        }

        private async UniTask AbortHostAsync(CancellationToken destroyToken)
        {
            networkManager.Shutdown();

            // Shutdown is deferred: an immediate retry would find Netcode still running and StartHost would fail.
            await UniTask.WaitWhile(() => networkManager.IsListening, cancellationToken: destroyToken);

            Scene lobbyScene = SceneManager.GetSceneByName(LobbySceneName);

            if (!lobbyScene.isLoaded)
                return;

            // Null when the scene is already being unloaded.
            AsyncOperation unload = SceneManager.UnloadSceneAsync(lobbyScene);

            if (unload != null)
                await unload.ToUniTask(cancellationToken: destroyToken);
        }
    }
}
