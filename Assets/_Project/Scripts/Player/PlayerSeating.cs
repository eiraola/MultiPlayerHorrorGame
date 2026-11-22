using ProjectScare.Core;
using Unity.Netcode;
using UnityEngine;

namespace ProjectScare.Player
{
    /// <summary>
    /// Switches the player between walking and sitting on a vehicle's seat. The host decides and keeps
    /// the state in a variable, and every instance switches mode when it changes. A seated player is
    /// always parented to the vehicle, but a parented one can also be walking inside it.
    /// </summary>
    public sealed class PlayerSeating : NetworkBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerInputSender inputSender;
        [SerializeField] private PlayerLook look;
        [SerializeField] private Transform cameraPivot;

        [Tooltip("How much lower the camera goes while seated.")]
        [SerializeField, Min(0f)] private float seatedCameraDrop = 0.6f;

        private readonly NetworkVariable<bool> isSeated = new();

        private Vector3 standingCameraPosition;

        public bool IsSeated => isSeated.Value;

        /// <summary>The seat that carries the player, used to stand up with the same key.</summary>
        public IInteractable CurrentSeat =>
            IsSeated && transform.parent != null ? transform.parent.GetComponentInChildren<IInteractable>() : null;

        private void Awake() => standingCameraPosition = cameraPivot.localPosition;

        public override void OnNetworkSpawn()
        {
            isSeated.OnValueChanged += HandleSeatedChanged;

            // The variable does not report the value it spawns with.
            if (IsSeated)
                HandleSeatedChanged(false, true);
        }

        public override void OnNetworkDespawn() => isSeated.OnValueChanged -= HandleSeatedChanged;

        /// <summary>Host only. Places the player on the seat, boarding the vehicle first if needed.</summary>
        public bool TrySit(NetworkObject vehicle, Transform seatAnchor)
        {
            if (!IsServer || IsSeated)
                return false;

            if (transform.parent != vehicle.transform && !NetworkObject.TrySetParent(vehicle, worldPositionStays: true))
                return false;

            // Seated first, so the controller is already off and does not push the player back.
            isSeated.Value = true;
            transform.position = seatAnchor.position;
            return true;
        }

        /// <summary>Host only. Leaves the seat and stands at the given point, still inside the vehicle.</summary>
        public void Stand(Vector3 standPosition)
        {
            if (!IsServer || !IsSeated)
                return;

            // Moved before standing, while the controller is still off and cannot push the player back.
            transform.position = standPosition;
            isSeated.Value = false;
        }

        // The view keeps pointing the same way; only what its yaw is relative to changes.
        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            if (IsOwner)
                look.SyncYawWithBody();
        }

        private void HandleSeatedChanged(bool previous, bool current)
        {
            characterController.enabled = !current;

            if (IsServer)
                motor.enabled = !current;

            if (!IsOwner)
                return;

            inputSender.SetMovementEnabled(!current);
            cameraPivot.localPosition = current ? standingCameraPosition + Vector3.down * seatedCameraDrop : standingCameraPosition;
        }
    }
}
