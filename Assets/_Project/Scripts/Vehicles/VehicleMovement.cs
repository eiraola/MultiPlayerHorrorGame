using UnityEngine;

namespace ProjectScare.Vehicles
{
    /// <summary>
    /// Driving rules shared by the host simulation and the tests. The host also uses them
    /// to clean the input it receives, since a client could send anything.
    /// </summary>
    public static class VehicleMovement
    {
        private const float FullTurn = 360f;

        public static float SanitizeAxis(float value) => float.IsFinite(value) ? Mathf.Clamp(value, -1f, 1f) : 0f;

        public static float NextYaw(float yaw, float steer, float turnSpeed, float deltaTime) =>
            Mathf.Repeat(yaw + steer * turnSpeed * deltaTime, FullTurn);

        public static Vector3 NextPosition(Vector3 position, float yaw, float throttle, float lift, VehicleSpeeds speeds, float deltaTime)
        {
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 next = position + (forward * (throttle * speeds.Move) + Vector3.up * (lift * speeds.Lift)) * deltaTime;
            next.y = Mathf.Clamp(next.y, speeds.MinHeight, speeds.MaxHeight);
            return next;
        }
    }

    /// <summary>Speeds and height limits, kept apart from the config asset so the rules can be tested.</summary>
    public readonly struct VehicleSpeeds
    {
        public float Move { get; }
        public float Lift { get; }
        public float MinHeight { get; }
        public float MaxHeight { get; }

        public VehicleSpeeds(float move, float lift, float minHeight, float maxHeight)
        {
            Move = move;
            Lift = lift;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
        }
    }
}
