using UnityEngine;

namespace ProjectScare.Player
{
    /// <summary>
    /// First person view angles in degrees. Yaw turns the body, pitch only the camera,
    /// and positive pitch looks down, as in Unity's X rotation.
    /// </summary>
    public readonly struct LookAngles
    {
        private const float FullTurn = 360f;

        public float Yaw { get; }
        public float Pitch { get; }

        public LookAngles(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = pitch;
        }

        public LookAngles Apply(Vector2 mouseDelta, float sensitivity, float minPitch, float maxPitch)
        {
            float yaw = Mathf.Repeat(Yaw + mouseDelta.x * sensitivity, FullTurn);

            // Moving the mouse up must look up, which is a lower pitch.
            float pitch = Mathf.Clamp(Pitch - mouseDelta.y * sensitivity, minPitch, maxPitch);

            return new LookAngles(yaw, pitch);
        }
    }
}
