using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectScare.Vehicles
{
    /// <summary>
    /// While the local player drives, sends the driving input to the host every network tick.
    /// It is sent even when it has not changed, so a lost message is fixed by the next one.
    /// </summary>
    public sealed class VehicleInputSender : NetworkBehaviour
    {
        [SerializeField] private VehicleSeat seat;
        [SerializeField] private VehicleMotor motor;

        [Tooltip("x steers, y drives forward and back.")]
        [SerializeField] private InputActionReference driveAction;

        [Tooltip("Positive goes up, negative goes down.")]
        [SerializeField] private InputActionReference liftAction;

        private Vector2 drive;
        private float lift;
        private uint sequence;
        private NetworkTickSystem tickSystem;

        public override void OnNetworkSpawn() => seat.DriverChanged += HandleDriverChanged;

        public override void OnNetworkDespawn()
        {
            seat.DriverChanged -= HandleDriverChanged;
            StopDriving();
        }

        private void HandleDriverChanged(ulong driverClientId)
        {
            if (driverClientId == NetworkManager.LocalClientId)
                StartDriving();
            else
                StopDriving();
        }

        private void StartDriving()
        {
            if (tickSystem != null)
                return;

            drive = Vector2.zero;
            lift = 0f;
            sequence = 0;

            driveAction.action.performed += HandleDrive;
            driveAction.action.canceled += HandleDrive;
            liftAction.action.performed += HandleLift;
            liftAction.action.canceled += HandleLift;
            driveAction.action.Enable();
            liftAction.action.Enable();

            // Kept in a field because the NetworkManager may be gone when this stops.
            tickSystem = NetworkManager.NetworkTickSystem;
            tickSystem.Tick += SendInput;
        }

        private void StopDriving()
        {
            if (tickSystem == null)
                return;

            driveAction.action.performed -= HandleDrive;
            driveAction.action.canceled -= HandleDrive;
            liftAction.action.performed -= HandleLift;
            liftAction.action.canceled -= HandleLift;
            driveAction.action.Disable();
            liftAction.action.Disable();

            tickSystem.Tick -= SendInput;
            tickSystem = null;
        }

        private void HandleDrive(InputAction.CallbackContext context) => drive = context.ReadValue<Vector2>();

        private void HandleLift(InputAction.CallbackContext context) => lift = context.ReadValue<float>();

        private void SendInput() => motor.SubmitDriveRpc(++sequence, drive, lift);
    }
}
