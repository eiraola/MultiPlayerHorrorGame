using ProjectScare.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectScare.Player
{
    /// <summary>
    /// Lets the local player use what it is looking at. While seated, the same key goes to the seat,
    /// so one press is always one request.
    /// </summary>
    public sealed class PlayerInteractor : NetworkBehaviour
    {
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private PlayerSeating seating;

        [Tooltip("Where the interaction ray starts and points to, usually the camera.")]
        [SerializeField] private Transform viewPoint;

        [SerializeField, Min(0f)] private float range = 2.5f;
        [SerializeField] private LayerMask interactableLayers = ~0;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                return;

            interactAction.action.performed += HandleInteract;
            interactAction.action.Enable();
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
                return;

            interactAction.action.performed -= HandleInteract;
            interactAction.action.Disable();
        }

        private void HandleInteract(InputAction.CallbackContext context)
        {
            if (seating.IsSeated)
            {
                seating.CurrentSeat?.Interact();
                return;
            }

            if (!Physics.Raycast(viewPoint.position, viewPoint.forward, out RaycastHit hit, range, interactableLayers, QueryTriggerInteraction.Collide))
                return;

            hit.collider.GetComponentInParent<IInteractable>()?.Interact();
        }
    }
}
