using UnityEngine;

namespace ProjectScare.Player
{
    /// <summary>Walks on the ground plane and falls with gravity. Ignores the pitch and the vertical input.</summary>
    public sealed class WalkMovement : IPlayerMovementMode
    {
        private readonly PlayerMovementConfig config;

        private float verticalSpeed;

        public WalkMovement(PlayerMovementConfig config) => this.config = config;

        public void Enter() => verticalSpeed = 0f;

        public Vector3 Velocity(Vector2 move, float yaw, float pitch, float vertical, bool isGrounded, float deltaTime)
        {
            verticalSpeed = PlayerMovement.NextVerticalSpeed(
                verticalSpeed, isGrounded, config.Gravity, config.GroundedVerticalSpeed, deltaTime);

            Vector3 velocity = PlayerMovement.HorizontalVelocity(move, yaw, config.WalkSpeed);
            velocity.y = verticalSpeed;
            return velocity;
        }
    }
}
