using UnityEngine;

namespace ProjectScare.Player
{
    [CreateAssetMenu(menuName = "ProjectScare/Config/Player Movement")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float walkSpeed = 3f;

        [Tooltip("Degrees turned per pixel of mouse movement.")]
        [SerializeField, Min(0f)] private float lookSensitivity = 0.1f;

        [Tooltip("Highest angle the camera can look up. Negative values look up.")]
        [SerializeField, Range(-89f, 0f)] private float minPitch = -80f;

        [Tooltip("Lowest angle the camera can look down.")]
        [SerializeField, Range(0f, 89f)] private float maxPitch = 80f;

        [SerializeField] private float gravity = -20f;

        [Tooltip("Small downward speed kept while grounded, so the controller stays on slopes and steps.")]
        [SerializeField, Range(-10f, 0f)] private float groundedVerticalSpeed = -2f;

        [SerializeField, Min(0f)] private float swimSpeed = 2.5f;

        [Tooltip("Speed when rising or sinking with the up and down keys.")]
        [SerializeField, Min(0f)] private float swimVerticalSpeed = 2f;

        public float WalkSpeed => walkSpeed;
        public float LookSensitivity => lookSensitivity;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public float Gravity => gravity;
        public float GroundedVerticalSpeed => groundedVerticalSpeed;
        public float SwimSpeed => swimSpeed;
        public float SwimVerticalSpeed => swimVerticalSpeed;
    }
}
