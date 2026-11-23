using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectScare.Player
{
    /// <summary>
    /// The only thing the local player does with its input: send it to the host every network tick.
    /// It is sent even when it has not changed, so a lost message is fixed by the next one.
    /// </summary>
    public sealed class PlayerInputSender : NetworkBehaviour
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference swimVerticalAction;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerMotor motor;

        private Vector2 move;
        private float vertical;
        private uint sequence;
        private NetworkTickSystem tickSystem;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                return;

            move = Vector2.zero;
            vertical = 0f;
            sequence = 0;

            moveAction.action.performed += HandleMove;
            moveAction.action.canceled += HandleMove;
            moveAction.action.Enable();

            swimVerticalAction.action.performed += HandleVertical;
            swimVerticalAction.action.canceled += HandleVertical;
            swimVerticalAction.action.Enable();

            // Kept in a field because the NetworkManager may be gone when this despawns.
            tickSystem = NetworkManager.NetworkTickSystem;
            tickSystem.Tick += SendInput;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
                return;

            moveAction.action.performed -= HandleMove;
            moveAction.action.canceled -= HandleMove;
            moveAction.action.Disable();

            swimVerticalAction.action.performed -= HandleVertical;
            swimVerticalAction.action.canceled -= HandleVertical;
            swimVerticalAction.action.Disable();

            if (tickSystem != null)
                tickSystem.Tick -= SendInput;

            tickSystem = null;
        }

        /// <summary>Owner only. Stops movement input, for example while seated.</summary>
        public void SetMovementEnabled(bool isEnabled)
        {
            move = Vector2.zero;
            vertical = 0f;

            if (isEnabled)
            {
                moveAction.action.Enable();
                swimVerticalAction.action.Enable();
            }
            else
            {
                moveAction.action.Disable();
                swimVerticalAction.action.Disable();
            }
        }

        private void HandleMove(InputAction.CallbackContext context) => move = context.ReadValue<Vector2>();

        private void HandleVertical(InputAction.CallbackContext context) => vertical = context.ReadValue<float>();

        private void SendInput() => motor.SubmitInputRpc(++sequence, move, look.WorldYaw, look.Pitch, vertical);
    }
}
