using UnityEngine;

namespace ProjectScare.Player
{
    /// <summary>
    /// Swims without gravity. Forward and back follow the view, pitch included, strafing stays level,
    /// and the vertical input moves straight up or down.
    /// </summary>
    public sealed class SwimMovement : IPlayerMovementMode
    {
        private readonly PlayerMovementConfig config;

        public SwimMovement(PlayerMovementConfig config) => this.config = config;

        public void Enter() { }

        public Vector3 Velocity(Vector2 move, float yaw, float pitch, float vertical, bool isGrounded, float deltaTime)
        {
            Vector3 forward = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * move.y;
            Vector3 side = Quaternion.Euler(0f, yaw, 0f) * Vector3.right * move.x;

            // Clamped so that swimming diagonally is not faster than swimming straight.
            Vector3 velocity = Vector3.ClampMagnitude(forward + side, 1f) * config.SwimSpeed;
            velocity.y += vertical * config.SwimVerticalSpeed;
            return velocity;
        }
    }
}
