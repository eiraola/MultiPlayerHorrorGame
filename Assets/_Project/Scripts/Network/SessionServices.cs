using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectScare.Network
{
    public sealed class SessionServices : MonoBehaviour
    {
        internal const string LobbySceneName = "MultiplayerLobby";
        internal const string RelayConnectionType = "dtls";
        internal const string RelayCodeKey = "relayCode";
        internal const string OwnerNameKey = "owner";
        internal const string RegionKey = "region";

        private static readonly TimeSpan LobbyCleanupTimeout = TimeSpan.FromSeconds(5);

        [SerializeField] private NetworkManager networkManager;

        private bool isOperationRunning;
        private Task signInTask;

        private void Awake()
        {
            isOperationRunning = false;
            signInTask = null;
        }

        public bool TryBeginOperation()
        {
            if (isOperationRunning)
                return false;

            isOperationRunning = true;
            return true;
        }

        public void EndOperation() => isOperationRunning = false;

        public UniTask EnsureSignedInAsync(CancellationToken ct)
        {
            // A second anonymous sign-in is rejected while another is running, so a retry joins the abandoned one.
            if (signInTask == null || signInTask.IsCompleted)
                signInTask = InitializeAndSignInAsync();

            return signInTask.AsUniTask().AttachExternalCancellation(ct);
        }

        public async UniTask ShutdownNetcodeAsync(CancellationToken destroyToken)
        {
            await StopNetcodeAsync(destroyToken);

            Scene lobbyScene = SceneManager.GetSceneByName(LobbySceneName);

            if (!lobbyScene.isLoaded)
                return;

            // Null when the scene is already being unloaded.
            AsyncOperation unload = SceneManager.UnloadSceneAsync(lobbyScene);

            if (unload != null)
                await unload.ToUniTask(cancellationToken: destroyToken);
        }

        // Leaves the lobby scene loaded, so it stays on screen until a fade covers it.
        public async UniTask StopNetcodeAsync(CancellationToken destroyToken)
        {
            // It may have stopped on its own already, for example when the host leaves.
            if (networkManager.IsListening)
                networkManager.Shutdown();

            // Shutdown is deferred: an immediate retry would find Netcode still running and starting it would fail.
            await UniTask.WaitWhile(
                () => networkManager.IsListening || networkManager.ShutdownInProgress,
                cancellationToken: destroyToken);
        }

        public async UniTask WaitForLobbyCleanupAsync(UniTask cleanup, CancellationToken destroyToken)
        {
            using var cleanupCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            using IDisposable timeout = cleanupCts.CancelAfterSlim(LobbyCleanupTimeout, DelayType.UnscaledDeltaTime);

            try
            {
                await cleanup.AttachExternalCancellation(cleanupCts.Token);
            }
            catch (OperationCanceledException)
            {
                // A slow cleanup must not block the retry. It keeps running, and otherwise the lobby expires.
                if (!destroyToken.IsCancellationRequested)
                    Debug.LogWarning("Cleaning up the lobby is taking too long.", this);
            }
        }

        // Only the reason of a lobby error is logged, as the service's text answers a request that may hold the password.
        internal static string DescribeFailure(Exception failure) =>
            failure is LobbyServiceException lobbyFailure
                ? $"{nameof(LobbyServiceException)}: {lobbyFailure.Reason}"
                : failure.ToString();

        private static async Task InitializeAndSignInAsync()
        {
            await UnityServices.InitializeAsync();

            if (AuthenticationService.Instance.IsSignedIn)
                return;

            // The cached token is shared by every build on this machine, and two instances must be different players.
            AuthenticationService.Instance.ClearSessionToken();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
