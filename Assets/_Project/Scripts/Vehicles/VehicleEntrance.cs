using ProjectScare.Player;
using Unity.Netcode;
using UnityEngine;

namespace ProjectScare.Vehicles
{
    /// <summary>
    /// Trigger across the vehicle's opening, with its forward axis pointing into the cabin. A player who
    /// leaves it towards the cabin rides with the vehicle, and one who leaves it the other way gets off.
    /// Deciding on exit means that standing in the opening changes nothing. Only the host decides.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class VehicleEntrance : NetworkBehaviour
    {
        private void OnTriggerExit(Collider other)
        {
            if (!IsSpawned || !IsServer || !other.TryGetComponent(out PlayerSeating player) || player.IsSeated)
                return;

            bool leftTowardsCabin = transform.InverseTransformPoint(other.transform.position).z > 0f;
            bool isAboard = other.transform.parent == NetworkObject.transform;

            if (leftTowardsCabin && !isAboard)
                player.NetworkObject.TrySetParent(NetworkObject, worldPositionStays: true);
            else if (!leftTowardsCabin && isAboard)
                player.NetworkObject.TryRemoveParent(worldPositionStays: true);
        }
    }
}
