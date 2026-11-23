using UnityEngine;

namespace ProjectScare.Player
{
    /// <summary>
    /// One way of moving the player, such as walking or swimming. <see cref="PlayerMotor"/> keeps the
    /// current one and swaps it when the player changes medium, so each mode only knows its own rules.
    /// </summary>
    public interface IPlayerMovementMode
    {
        /// <summary>Called when the mode becomes the current one, to drop what was left from its last use.</summary>
        void Enter();

        /// <param name="move">Horizontal input: x strafes, y goes forward.</param>
        /// <param name="yaw">World yaw of the view.</param>
        /// <param name="pitch">Pitch of the view. Negative values look up.</param>
        /// <param name="vertical">Up and down input, from -1 to 1.</param>
        Vector3 Velocity(Vector2 move, float yaw, float pitch, float vertical, bool isGrounded, float deltaTime);
    }
}
