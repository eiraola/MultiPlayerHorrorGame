using UnityEngine;

namespace ProjectScare.Player
{
    /// <summary>
    /// Trigger that switches the player's movement: entering it sets one mode and leaving it sets the
    /// other. Keep it on the Ignore Raycast layer so the interaction ray goes through it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class MovementZone : MonoBehaviour
    {
        [Tooltip("On: swim inside and walk after leaving. Off: walk inside and swim after leaving.")]
        [SerializeField] private bool swimInside;

        public bool SwimInside => swimInside;
    }
}
