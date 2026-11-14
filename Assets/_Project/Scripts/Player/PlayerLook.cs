using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectScare.Player
{
    /// <summary>
    /// Turns the local player's view as soon as the mouse moves, without waiting for the host.
    /// Only enabled on the owner, by <see cref="PlayerPresentation"/>.
    /// </summary>
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private Transform body;
        [SerializeField] private Transform cameraPivot;

        private LookAngles angles;

        public float Yaw => angles.Yaw;

        private void OnEnable()
        {
            angles = new LookAngles(body.eulerAngles.y, 0f);
            ApplyAngles();

            lookAction.action.performed += HandleLook;
            lookAction.action.Enable();
        }

        private void OnDisable()
        {
            lookAction.action.performed -= HandleLook;
            lookAction.action.Disable();
        }

        // The mouse delta is already the movement of this frame, so it is not scaled by time.
        private void HandleLook(InputAction.CallbackContext context)
        {
            angles = angles.Apply(context.ReadValue<Vector2>(), config.LookSensitivity, config.MinPitch, config.MaxPitch);
            ApplyAngles();
        }

        private void ApplyAngles()
        {
            body.rotation = Quaternion.Euler(0f, angles.Yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(angles.Pitch, 0f, 0f);
        }
    }
}
