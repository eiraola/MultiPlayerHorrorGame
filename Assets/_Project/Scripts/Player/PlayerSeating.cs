using ProjectScare.Core;
using Unity.Netcode;
using UnityEngine;

namespace ProjectScare.Player
{
    /// <summary>
    /// Switches the player between walking and being carried by a seat. Being seated means being
    /// parented to another network object: the host does the parenting, and every instance switches
    /// mode when the parent change reaches it.
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

        private Vector3 standingCameraPosition;

        public bool IsSeated { get; private set; }

        /// <summary>The seat that carries the player, used to stand up with the same key.</summary>
        public IInteractable CurrentSeat { get; private set; }

        private void Awake() => standingCameraPosition = cameraPivot.localPosition;

        /// <summary>Host only. Parents the player to the vehicle and places it on the seat.</summary>
        public bool TrySit(NetworkObject vehicle, Transform seatAnchor)
        {
            if (!IsServer || IsSeated || !NetworkObject.TrySetParent(vehicle, worldPositionStays: true))
                return false;

            transform.position = seatAnchor.position;
            return true;
        }

        /// <summary>Host only. Leaves the seat and stands at the given point.</summary>
        public void Stand(Vector3 standPosition)
        {
            if (!IsServer || !IsSeated)
                return;

            // Moved before unparenting, while the controller is still off and cannot push the player back.
            transform.position = standPosition;
            NetworkObject.TryRemoveParent(worldPositionStays: true);
        }

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            IsSeated = parentNetworkObject != null;
            CurrentSeat = IsSeated ? parentNetworkObject.GetComponentInChildren<IInteractable>() : null;

            characterController.enabled = !IsSeated;

            if (IsServer)
                motor.enabled = !IsSeated;

            if (!IsOwner)
                return;

            inputSender.SetMovementEnabled(!IsSeated);
            cameraPivot.localPosition = IsSeated ? standingCameraPosition + Vector3.down * seatedCameraDrop : standingCameraPosition;

            // The view keeps pointing the same way; only what its yaw is relative to changes.
            look.SyncYawWithBody();
        }
    }
}
