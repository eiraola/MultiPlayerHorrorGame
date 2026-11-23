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
    /// How the input becomes movement depends on the current mode, walking or swimming, which
    /// <see cref="MovementZone"/> triggers switch. The game takes place at sea, so players start swimming.
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
        private float pitch;
        private float vertical;
        private Tween yawTween;

        private WalkMovement walkMovement;
        private SwimMovement swimMovement;
        private IPlayerMovementMode currentMovement;

        private float ParentYaw => transform.parent != null ? transform.parent.eulerAngles.y : 0f;

        private void Awake()
        {
            walkMovement = new WalkMovement(config);
            swimMovement = new SwimMovement(config);
            SetMovement(swimMovement);
        }

        public override void OnNetworkSpawn()
        {
            move = Vector2.zero;
            lastSequence = 0;
            pitch = 0f;
            vertical = 0f;
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
        public void SubmitInputRpc(
            uint sequence, Vector2 newMove, float newWorldYaw, float newPitch, float newVertical, RpcParams rpcParams = default)
        {
            // Unreliable messages can arrive late and out of order: an older one would undo a newer input.
            if (rpcParams.Receive.SenderClientId != OwnerClientId || sequence <= lastSequence)
                return;

            lastSequence = sequence;
            move = PlayerMovement.SanitizeMove(newMove);
            worldYaw = PlayerMovement.SanitizeYaw(newWorldYaw);
            pitch = PlayerMovement.SanitizePitch(newPitch);
            vertical = PlayerMovement.SanitizeAxis(newVertical);
            yaw.Value = PlayerMovement.SanitizeYaw(worldYaw - ParentYaw);
        }

        // Per frame and only on the host: moving on the 30 Hz tick would look choppy on the host,
        // which owns the transform and does not interpolate it.
        private void Update()
        {
            if (!IsSpawned)
                return;

            float deltaTime = Time.deltaTime;
            Vector3 velocity = currentMovement.Velocity(
                move, worldYaw, pitch, vertical, characterController.isGrounded, deltaTime);
            characterController.Move(velocity * deltaTime);
        }

        // Handled on every instance and even before spawning, so a player who appears inside a zone
        // still gets its mode. Trigger messages also reach this component while it is disabled.
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out MovementZone zone))
                SetMovement(zone.SwimInside ? swimMovement : walkMovement);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out MovementZone zone))
                SetMovement(zone.SwimInside ? walkMovement : swimMovement);
        }

        private void SetMovement(IPlayerMovementMode next)
        {
            if (next == currentMovement)
                return;

            currentMovement = next;
            currentMovement.Enter();
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
