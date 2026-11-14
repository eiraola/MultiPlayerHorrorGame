using ProjectScare.Core;
using UnityEngine;

namespace ProjectScare.SceneFlow
{
    /// <summary>
    /// Turns the persistent camera off while the local player has its own, so only one camera
    /// renders and only one audio listener is active.
    /// </summary>
    public sealed class MenuCameraSwitch : MonoBehaviour
    {
        [SerializeField] private Camera menuCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private BoolValueBus localPlayerSpawnedBus;

        private void OnEnable()
        {
            localPlayerSpawnedBus.Changed += Apply;

            // The player may have spawned before this was enabled.
            Apply(localPlayerSpawnedBus.Value);
        }

        private void OnDisable() => localPlayerSpawnedBus.Changed -= Apply;

        private void Apply(bool isLocalPlayerSpawned)
        {
            menuCamera.enabled = !isLocalPlayerSpawned;
            audioListener.enabled = !isLocalPlayerSpawned;
        }
    }
}
