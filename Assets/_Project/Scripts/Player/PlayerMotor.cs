using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

namespace ProjectScare.Player
{
    /// <summary>
    /// Moves the player on the host from the input its owner sends. The position reaches everyone
    /// through a server NetworkTransform, and the yaw through a variable that every non-owner applies.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : NetworkBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private CharacterController characterController;

        private readonly NetworkVariable<float> yaw = new();

        private Vector2 move;
        private uint lastSequence;
        private float verticalSpeed;
        private Tween yawTween;

        public override void OnNetworkSpawn()
        {
            move = Vector2.zero;
            lastSequence = 0;
            verticalSpeed = 0f;

            yaw.OnValueChanged += HandleYawChanged;

            if (IsServer)
                yaw.Value = PlayerMovement.SanitizeYaw(transform.eulerAngles.y);

            // Only the host simulates.
            enabled = IsServer;
        }

        public override void OnNetworkDespawn()
        {
            yaw.OnValueChanged -= HandleYawChanged;
            yawTween?.Kill();
            yawTween = null;
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        public void SubmitInputRpc(uint sequence, Vector2 newMove, float newYaw, RpcParams rpcParams = default)
        {
            // Unreliable messages can arrive late and out of order: an older one would undo a newer input.
            if (rpcParams.Receive.SenderClientId != OwnerClientId || sequence <= lastSequence)
                return;

            lastSequence = sequence;
            move = PlayerMovement.SanitizeMove(newMove);
            yaw.Value = PlayerMovement.SanitizeYaw(newYaw);
        }

        // Per frame and only on the host: moving on the 30 Hz tick would look choppy on the host,
        // which owns the transform and does not interpolate it.
        private void Update()
        {
            if (!IsSpawned)
                return;

            float deltaTime = Time.deltaTime;
            verticalSpeed = PlayerMovement.NextVerticalSpeed(
                verticalSpeed, characterController.isGrounded, config.Gravity, config.GroundedVerticalSpeed, deltaTime);

            Vector3 velocity = PlayerMovement.HorizontalVelocity(move, yaw.Value, config.WalkSpeed);
            velocity.y = verticalSpeed;
            characterController.Move(velocity * deltaTime);
        }

        // The owner already turned on its own, and the host would only drag it back a tick.
        private void HandleYawChanged(float previous, float current)
        {
            if (IsOwner)
                return;

            // Eased over one tick, so the remote player turns smoothly between updates.
            yawTween?.Kill();
            yawTween = transform
                .DORotateQuaternion(Quaternion.Euler(0f, current, 0f), 1f / NetworkManager.NetworkConfig.TickRate)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }
    }
}
