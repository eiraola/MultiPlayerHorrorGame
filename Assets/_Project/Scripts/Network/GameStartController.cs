using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectScare.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectScare.Network
{
    /// <summary>
    /// Takes every player from the lobby to MainGame when the countdown ends. It lives in
    /// Bootstrap because it has to outlast the lobby scene it unloads.
    /// </summary>
    public sealed class GameStartController : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private IntValueBus countdownBus;
        [SerializeField] private EventBus fadeToBlackRequestBus;
        [SerializeField] private EventBus fadedToBlackBus;
        [SerializeField] private EventBus fadeFromBlackRequestBus;

        // Kept in a field because Netcode clears its own property before raising OnClientStopped.
        private NetworkSceneManager sceneManager;
        private bool isWaitingForBlack;

        private bool IsNetcodeRunning => networkManager.IsListening && !networkManager.ShutdownInProgress;

        private void Awake()
        {
            sceneManager = null;
            isWaitingForBlack = false;
        }

        private void OnEnable()
        {
            countdownBus.Changed += HandleCountdownChanged;
            fadedToBlackBus.Raised += HandleFadedToBlack;
            networkManager.OnClientStarted += HandleClientStarted;
            networkManager.OnClientStopped += HandleClientStopped;
        }

        private void OnDisable()
        {
            countdownBus.Changed -= HandleCountdownChanged;
            fadedToBlackBus.Raised -= HandleFadedToBlack;
            StopListeningToSceneEvents();

            // The NetworkManager may be destroyed first when the application closes.
            if (networkManager == null)
                return;

            networkManager.OnClientStarted -= HandleClientStarted;
            networkManager.OnClientStopped -= HandleClientStopped;
        }

        private void HandleClientStarted()
        {
            StopListeningToSceneEvents();
            sceneManager = networkManager.SceneManager;
            sceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
            sceneManager.OnUnloadEventCompleted += HandleUnloadEventCompleted;
        }

        private void HandleClientStopped(bool _)
        {
            StopListeningToSceneEvents();
            isWaitingForBlack = false;
        }

        private void StopListeningToSceneEvents()
        {
            if (sceneManager == null)
                return;

            sceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
            sceneManager.OnUnloadEventCompleted -= HandleUnloadEventCompleted;
            sceneManager = null;
        }

        private void HandleCountdownChanged(int countdown)
        {
            if (countdown != LobbyRules.CountdownFinished)
                return;

            // Set first because the fade can finish inside the request, if the screen is already black.
            isWaitingForBlack = networkManager.IsServer;
            fadeToBlackRequestBus.Raise();
        }

        private void HandleFadedToBlack()
        {
            if (!isWaitingForBlack || !IsNetcodeRunning)
                return;

            isWaitingForBlack = false;
            LoadMainGame();
        }

        private void LoadMainGame()
        {
            try
            {
                // Additive because Single would also unload the persistent Bootstrap scene.
                EnsureStarted(sceneManager.LoadScene(SessionServices.MainGameSceneName, LoadSceneMode.Additive));
            }
            catch (Exception e)
            {
                AbortSession(e);
            }
        }

        private void HandleLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            if (sceneName != SessionServices.MainGameSceneName || !networkManager.IsServer)
                return;

            DisconnectClientsLeftBehind(completed);
            UnloadLobbyAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid UnloadLobbyAsync(CancellationToken destroyToken)
        {
            try
            {
                // Netcode is still closing the load event inside its callback, and would refuse another one.
                await UniTask.Yield(destroyToken);

                if (IsNetcodeRunning)
                    EnsureStarted(sceneManager.UnloadScene(SceneManager.GetSceneByName(SessionServices.LobbySceneName)));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                AbortSession(e);
            }
        }

        private void HandleUnloadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            if (sceneName != SessionServices.LobbySceneName)
                return;

            if (networkManager.IsServer)
                DisconnectClientsLeftBehind(completed);

            // Netcode only changes the active scene on Single loads.
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SessionServices.MainGameSceneName));
            fadeFromBlackRequestBus.Raise();
        }

        // Uses the completed list because the timed out one only names clients that already disconnected,
        // so a client that hangs would stay connected. Once disconnected, it returns to the menu on its own.
        private void DisconnectClientsLeftBehind(List<ulong> completed)
        {
            var connectedClients = new List<ulong>(networkManager.ConnectedClientsIds);

            foreach (ulong clientId in connectedClients)
            {
                if (clientId != networkManager.LocalClientId && !completed.Contains(clientId))
                    networkManager.DisconnectClient(clientId);
            }
        }

        private static void EnsureStarted(SceneEventProgressStatus status)
        {
            if (status != SceneEventProgressStatus.Started)
                throw new InvalidOperationException($"The scene event was refused: {status}.");
        }

        // Stopping Netcode sends everyone back to the menu, so nobody stays on a black screen.
        private void AbortSession(Exception failure)
        {
            Debug.LogError($"Starting the game failed: {failure}", this);
            networkManager.Shutdown();
        }
    }
}
