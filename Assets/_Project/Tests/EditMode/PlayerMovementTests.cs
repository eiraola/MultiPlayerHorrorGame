using NUnit.Framework;
using ProjectScare.Player;
using UnityEngine;

namespace ProjectScare.Tests.EditMode
{
    public class PlayerMovementTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void SanitizeMove_KeepsValidInput()
        {
            Vector2 move = new(0.3f, -0.5f);

            Assert.AreEqual(move, PlayerMovement.SanitizeMove(move));
        }

        [Test]
        public void SanitizeMove_LimitsSpeedToOne()
        {
            Vector2 result = PlayerMovement.SanitizeMove(new Vector2(10f, 10f));

            Assert.AreEqual(1f, result.magnitude, Tolerance);
        }

        [TestCase(float.NaN, 0f)]
        [TestCase(0f, float.PositiveInfinity)]
        public void SanitizeMove_RejectsInvalidNumbers(float x, float y)
        {
            Assert.AreEqual(Vector2.zero, PlayerMovement.SanitizeMove(new Vector2(x, y)));
        }

        [TestCase(370f, 10f)]
        [TestCase(-90f, 270f)]
        [TestCase(float.NaN, 0f)]
        public void SanitizeYaw_StaysInOneTurn(float yaw, float expected)
        {
            Assert.AreEqual(expected, PlayerMovement.SanitizeYaw(yaw), Tolerance);
        }

        [Test]
        public void HorizontalVelocity_ForwardFollowsYaw()
        {
            Vector3 velocity = PlayerMovement.HorizontalVelocity(Vector2.up, 90f, 3f);

            Assert.AreEqual(3f, velocity.x, Tolerance);
            Assert.AreEqual(0f, velocity.y, Tolerance);
            Assert.AreEqual(0f, velocity.z, Tolerance);
        }

        [Test]
        public void NextVerticalSpeed_FallsInTheAir_AndResetsOnTheGround()
        {
            Assert.AreEqual(-3f, PlayerMovement.NextVerticalSpeed(-1f, false, -20f, -2f, 0.1f), Tolerance);
            Assert.AreEqual(-2f, PlayerMovement.NextVerticalSpeed(-10f, true, -20f, -2f, 0.1f), Tolerance);
        }

        [TestCase(-120f, -90f)]
        [TestCase(45f, 45f)]
        [TestCase(float.NaN, 0f)]
        public void SanitizePitch_StaysWithinStraightUpAndDown(float pitch, float expected)
        {
            Assert.AreEqual(expected, PlayerMovement.SanitizePitch(pitch), Tolerance);
        }

        [TestCase(3f, 1f)]
        [TestCase(-0.5f, -0.5f)]
        [TestCase(float.PositiveInfinity, 0f)]
        public void SanitizeAxis_StaysWithinOne(float value, float expected)
        {
            Assert.AreEqual(expected, PlayerMovement.SanitizeAxis(value), Tolerance);
        }
    }
}
