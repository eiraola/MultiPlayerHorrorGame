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
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerMotor motor;

        private Vector2 move;
        private uint sequence;
        private NetworkTickSystem tickSystem;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                return;

            move = Vector2.zero;
            sequence = 0;

            moveAction.action.performed += HandleMove;
            moveAction.action.canceled += HandleMove;
            moveAction.action.Enable();

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

            if (tickSystem != null)
                tickSystem.Tick -= SendInput;

            tickSystem = null;
        }

        /// <summary>Owner only. Stops walking input, for example while seated.</summary>
        public void SetMovementEnabled(bool isEnabled)
        {
            move = Vector2.zero;

            if (isEnabled)
                moveAction.action.Enable();
            else
                moveAction.action.Disable();
        }

        private void HandleMove(InputAction.CallbackContext context) => move = context.ReadValue<Vector2>();

        private void SendInput() => motor.SubmitInputRpc(++sequence, move, look.Yaw);
    }
}
