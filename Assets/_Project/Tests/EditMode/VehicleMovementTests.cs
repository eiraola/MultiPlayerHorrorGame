using NUnit.Framework;
using ProjectScare.Vehicles;
using UnityEngine;

namespace ProjectScare.Tests.EditMode
{
    public class VehicleMovementTests
    {
        private const float Tolerance = 0.001f;

        private static readonly VehicleSpeeds Speeds = new(move: 4f, lift: 2f, minHeight: 0f, maxHeight: 10f);

        [TestCase(0.5f, 0.5f)]
        [TestCase(3f, 1f)]
        [TestCase(-3f, -1f)]
        [TestCase(float.NaN, 0f)]
        [TestCase(float.NegativeInfinity, 0f)]
        public void SanitizeAxis_KeepsValuesBetweenMinusOneAndOne(float value, float expected)
        {
            Assert.AreEqual(expected, VehicleMovement.SanitizeAxis(value), Tolerance);
        }

        [Test]
        public void NextYaw_TurnsAtTurnSpeed()
        {
            Assert.AreEqual(15f, VehicleMovement.NextYaw(0f, 1f, 30f, 0.5f), Tolerance);
        }

        [Test]
        public void NextYaw_WrapsAround()
        {
            Assert.AreEqual(350f, VehicleMovement.NextYaw(5f, -1f, 30f, 0.5f), Tolerance);
        }

        [Test]
        public void NextPosition_MovesAlongYaw()
        {
            Vector3 result = VehicleMovement.NextPosition(new Vector3(0f, 1f, 0f), 90f, 1f, 0f, Speeds, 1f);

            Assert.AreEqual(4f, result.x, Tolerance);
            Assert.AreEqual(1f, result.y, Tolerance);
            Assert.AreEqual(0f, result.z, Tolerance);
        }

        [Test]
        public void NextPosition_ReversesWithNegativeThrottle()
        {
            Vector3 result = VehicleMovement.NextPosition(new Vector3(0f, 1f, 0f), 0f, -1f, 0f, Speeds, 0.5f);

            Assert.AreEqual(-2f, result.z, Tolerance);
        }

        [Test]
        public void NextPosition_WithoutInputStaysStill()
        {
            Vector3 start = new(3f, 2f, -1f);

            Assert.AreEqual(start, VehicleMovement.NextPosition(start, 45f, 0f, 0f, Speeds, 1f));
        }

        [TestCase(1f, 10f)]
        [TestCase(-1f, 0f)]
        public void NextPosition_KeepsHeightWithinLimits(float lift, float expectedHeight)
        {
            Vector3 result = VehicleMovement.NextPosition(new Vector3(0f, 5f, 0f), 0f, 0f, lift, Speeds, 100f);

            Assert.AreEqual(expectedHeight, result.y, Tolerance);
        }
    }
}
