using UnityEngine;

namespace ProjectScare.Player
{
    /// <summary>
    /// Movement rules shared by the host simulation and the tests. The host also uses them
    /// to clean the input it receives, since a client could send anything.
    /// </summary>
    public static class PlayerMovement
    {
        private const float FullTurn = 360f;

        public static Vector2 SanitizeMove(Vector2 move)
        {
            if (!float.IsFinite(move.x) || !float.IsFinite(move.y))
                return Vector2.zero;

            return move.sqrMagnitude > 1f ? move.normalized : move;
        }

        public static float SanitizeYaw(float yaw) => float.IsFinite(yaw) ? Mathf.Repeat(yaw, FullTurn) : 0f;

        public static Vector3 HorizontalVelocity(Vector2 move, float yaw, float speed) =>
            Quaternion.Euler(0f, yaw, 0f) * new Vector3(move.x, 0f, move.y) * speed;

        public static float NextVerticalSpeed(float current, bool isGrounded, float gravity, float groundedSpeed, float deltaTime) =>
            isGrounded ? groundedSpeed : current + gravity * deltaTime;
    }
}
