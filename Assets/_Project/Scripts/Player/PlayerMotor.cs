using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

namespace ProjectScare.Player
{
    /// <summary>
    /// Moves the player on the host from the input its owner sends. The position reaches everyone
    /// through a server NetworkTransform, and the yaw through a variable that every non-owner applies.
    /// The owner sends its yaw in world space, which stays right while the player boards or leaves a
    /// vehicle, when the owner and the host briefly disagree on what the player is parented to.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : NetworkBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private CharacterController characterController;

        // Relative to the parent, so a remote passenger turns along with the vehicle.
        private readonly NetworkVariable<float> yaw = new();

        private Vector2 move;
        private uint lastSequence;
        private float worldYaw;
        private float verticalSpeed;
        private Tween yawTween;

        private float ParentYaw => transform.parent != null ? transform.parent.eulerAngles.y : 0f;

        public override void OnNetworkSpawn()
        {
            move = Vector2.zero;
            lastSequence = 0;
            verticalSpeed = 0f;
            worldYaw = PlayerMovement.SanitizeYaw(transform.eulerAngles.y);

            yaw.OnValueChanged += HandleYawChanged;

            if (IsServer)
                yaw.Value = worldYaw;

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
        public void SubmitInputRpc(uint sequence, Vector2 newMove, float newWorldYaw, RpcParams rpcParams = default)
        {
            // Unreliable messages can arrive late and out of order: an older one would undo a newer input.
            if (rpcParams.Receive.SenderClientId != OwnerClientId || sequence <= lastSequence)
                return;

            lastSequence = sequence;
            move = PlayerMovement.SanitizeMove(newMove);
            worldYaw = PlayerMovement.SanitizeYaw(newWorldYaw);
            yaw.Value = PlayerMovement.SanitizeYaw(worldYaw - ParentYaw);
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

            Vector3 velocity = PlayerMovement.HorizontalVelocity(move, worldYaw, config.WalkSpeed);
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
                .DOLocalRotateQuaternion(Quaternion.Euler(0f, current, 0f), 1f / NetworkManager.NetworkConfig.TickRate)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }
    }
}
