using ProjectScare.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectScare.Player
{
    /// <summary>
    /// Decides how this player is seen: the owner looks through its camera and does not see its
    /// own body, while every other instance shows the capsule and the front marker.
    /// </summary>
    public sealed class PlayerPresentation : NetworkBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private PlayerLook look;

        [Tooltip("Capsule and front marker. The owner keeps their shadow only.")]
        [SerializeField] private Renderer[] bodyRenderers;

        [Tooltip("Tells the rest of the game that the local player now has its own camera.")]
        [SerializeField] private BoolValueBus localPlayerSpawnedBus;

        public override void OnNetworkSpawn()
        {
            bool isLocal = IsOwner;

            playerCamera.enabled = isLocal;
            audioListener.enabled = isLocal;
            look.enabled = isLocal;

            foreach (Renderer bodyRenderer in bodyRenderers)
                bodyRenderer.shadowCastingMode = isLocal ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;

            if (!isLocal)
                return;

            SetCursorLocked(true);
            localPlayerSpawnedBus.Set(true);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
                return;

            look.enabled = false;
            SetCursorLocked(false);
            localPlayerSpawnedBus.Set(false);
        }

        private static void SetCursorLocked(bool isLocked)
        {
            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLocked;
        }
    }
}
