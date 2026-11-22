using Unity.Netcode;
using UnityEngine;

namespace ProjectScare.Vehicles
{
    /// <summary>
    /// Moves the vehicle on the host from the input its driver sends. The position and yaw reach
    /// everyone through a server NetworkTransform, and the players inside ride along as children.
    /// </summary>
    public sealed class VehicleMotor : NetworkBehaviour
    {
        [SerializeField] private VehicleConfig config;
        [SerializeField] private VehicleSeat seat;

        private Vector2 drive;
        private float lift;
        private uint lastSequence;

        public override void OnNetworkSpawn()
        {
            ResetInput();
            seat.DriverChanged += HandleDriverChanged;

            // Only the host simulates.
            enabled = IsServer;
        }

        public override void OnNetworkDespawn() => seat.DriverChanged -= HandleDriverChanged;

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        public void SubmitDriveRpc(uint sequence, Vector2 newDrive, float newLift, RpcParams rpcParams = default)
        {
            // Unreliable messages can arrive late and out of order, and a former driver's can still be on the way.
            if (rpcParams.Receive.SenderClientId != seat.DriverClientId || sequence <= lastSequence)
                return;

            lastSequence = sequence;
            drive = new Vector2(VehicleMovement.SanitizeAxis(newDrive.x), VehicleMovement.SanitizeAxis(newDrive.y));
            lift = VehicleMovement.SanitizeAxis(newLift);
        }

        // Per frame and only on the host, for the same reason as the player's motor: moving on the
        // 30 Hz tick would look choppy on the host, which does not interpolate what it owns.
        private void Update()
        {
            if (!IsSpawned || (drive == Vector2.zero && lift == 0f))
                return;

            float deltaTime = Time.deltaTime;
            var speeds = new VehicleSpeeds(config.MoveSpeed, config.LiftSpeed, config.MinHeight, config.MaxHeight);

            float yaw = VehicleMovement.NextYaw(transform.eulerAngles.y, drive.x, config.TurnSpeed, deltaTime);
            Vector3 position = VehicleMovement.NextPosition(transform.position, yaw, drive.y, lift, speeds, deltaTime);

            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            // Transforms are not synced to physics automatically, and the passengers' controllers
            // must find the floor and the barriers where they are in this frame.
            Physics.SyncTransforms();
        }

        // A new driver starts from rest, and so does a vehicle left without one.
        private void HandleDriverChanged(ulong driverClientId) => ResetInput();

        private void ResetInput()
        {
            drive = Vector2.zero;
            lift = 0f;
            lastSequence = 0;
        }
    }
}
