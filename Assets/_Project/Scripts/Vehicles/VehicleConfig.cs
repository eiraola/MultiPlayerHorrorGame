using UnityEngine;

namespace ProjectScare.Vehicles
{
    [CreateAssetMenu(menuName = "ProjectScare/Config/Vehicle")]
    public sealed class VehicleConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float moveSpeed = 4f;

        [Tooltip("Degrees turned per second at full steering.")]
        [SerializeField, Min(0f)] private float turnSpeed = 30f;

        [SerializeField, Min(0f)] private float liftSpeed = 2f;

        [Tooltip("Lowest height the vehicle's pivot can go down to.")]
        [SerializeField] private float minHeight = 0f;

        [Tooltip("Highest height the vehicle's pivot can go up to.")]
        [SerializeField] private float maxHeight = 10f;

        [Tooltip("Furthest a player can be from the seat to sit down.")]
        [SerializeField, Min(0f)] private float maxSeatDistance = 3f;

        public float MoveSpeed => moveSpeed;
        public float TurnSpeed => turnSpeed;
        public float LiftSpeed => liftSpeed;
        public float MinHeight => minHeight;
        public float MaxHeight => maxHeight;
        public float MaxSeatDistance => maxSeatDistance;

        private void OnValidate() => maxHeight = Mathf.Max(maxHeight, minHeight);
    }
}
