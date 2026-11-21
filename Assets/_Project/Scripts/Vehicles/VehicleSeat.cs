using System;
using ProjectScare.Core;
using ProjectScare.Player;
using Unity.Netcode;
using UnityEngine;

namespace ProjectScare.Vehicles
{
    /// <summary>
    /// The driver's seat. Anyone can ask to sit down or stand up; the host decides and keeps
    /// who is driving in a variable that every client reads.
    /// </summary>
    public sealed class VehicleSeat : NetworkBehaviour, IInteractable
    {
        public const ulong NoDriver = ulong.MaxValue;

        [SerializeField] private VehicleConfig config;

        [Tooltip("Where the seated player is placed.")]
        [SerializeField] private Transform seatAnchor;

        [Tooltip("Where the player stands after leaving the seat.")]
        [SerializeField] private Transform exitPoint;

        private readonly NetworkVariable<ulong> driverClientId = new(NoDriver);

        private NetworkManager networkManager;

        /// <summary>Raised on every instance with the new driver, or <see cref="NoDriver"/>.</summary>
        public event Action<ulong> DriverChanged;

        public ulong DriverClientId => driverClientId.Value;

        public override void OnNetworkSpawn()
        {
            driverClientId.OnValueChanged += HandleDriverChanged;

            if (!IsServer)
                return;

            // Kept in a field because the NetworkManager may be gone when this despawns.
            networkManager = NetworkManager;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnect;
        }

        public override void OnNetworkDespawn()
        {
            driverClientId.OnValueChanged -= HandleDriverChanged;

            if (networkManager != null)
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnect;

            networkManager = null;
        }

        public void Interact() => RequestToggleRpc();

        [Rpc(SendTo.Server)]
        private void RequestToggleRpc(RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;

            if (driverClientId.Value == senderClientId)
                StandUp();
            else if (driverClientId.Value == NoDriver)
                TrySeat(senderClientId);
        }

        private void TrySeat(ulong clientId)
        {
            if (!TryGetSeating(clientId, out PlayerSeating seating))
                return;

            float maxDistance = config.MaxSeatDistance;

            if ((seating.transform.position - seatAnchor.position).sqrMagnitude > maxDistance * maxDistance)
                return;

            if (seating.TrySit(NetworkObject, seatAnchor))
                driverClientId.Value = clientId;
        }

        private void StandUp()
        {
            if (TryGetSeating(driverClientId.Value, out PlayerSeating seating))
                seating.Stand(exitPoint.position);

            driverClientId.Value = NoDriver;
        }

        private bool TryGetSeating(ulong clientId, out PlayerSeating seating)
        {
            seating = null;

            return NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client)
                && client.PlayerObject != null
                && client.PlayerObject.TryGetComponent(out seating);
        }

        // Netcode destroys the player of a client that leaves, so only the seat needs freeing.
        private void HandleClientDisconnect(ulong clientId)
        {
            if (clientId == driverClientId.Value)
                driverClientId.Value = NoDriver;
        }

        private void HandleDriverChanged(ulong previous, ulong current) => DriverChanged?.Invoke(current);
    }
}
