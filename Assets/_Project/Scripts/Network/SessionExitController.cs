using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectScare.Core;
using Unity.Netcode;
using UnityEngine;

namespace ProjectScare.Network
{
    /// <summary>
    /// Takes the player back to the main menu when they leave the room, or when the session
    /// stops on its own, for example because the host left.
    /// </summary>
    public sealed class SessionExitController : MonoBehaviour
    {
        [SerializeField] private string menuSceneName = "Menu";
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private SessionServices sessionServices;
        [SerializeField] private EventBus leaveRequestBus;
        [SerializeField] private EventBus sessionEndedBus;
        [SerializeField] private StringEventBus sceneRequestBus;

        private void OnEnable()
        {
            leaveRequestBus.Raised += ReturnToMenu;
            networkManager.OnClientStopped += HandleClientStopped;
        }

        private void OnDisable()
        {
            leaveRequestBus.Raised -= ReturnToMenu;

            // The NetworkManager may be destroyed first when the application closes.
            if (networkManager != null)
                networkManager.OnClientStopped -= HandleClientStopped;
        }

        private void HandleClientStopped(bool _) => ReturnToMenu();

        private void ReturnToMenu()
        {
            // Refused while creating or joining a room, whose own failure handling stops Netcode,
            // and while this exit runs, since stopping Netcode here raises OnClientStopped again.
            if (!sessionServices.TryBeginOperation())
                return;

            ReturnToMenuAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid ReturnToMenuAsync(CancellationToken destroyToken)
        {
            try
            {
                // OnClientStopped is raised from inside the shutdown, which must end before stopping again.
                await UniTask.Yield(destroyToken);
                await sessionServices.StopNetcodeAsync(destroyToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogError($"Leaving the room failed: {e}", this);
            }

            if (destroyToken.IsCancellationRequested)
                return;

            Debug.Log("Left the room.", this);
            sessionEndedBus.Raise();
            sessionServices.EndOperation();

            // The scene change fades to black before unloading the lobby scene.
            sceneRequestBus.Raise(menuSceneName);
        }
    }
}
