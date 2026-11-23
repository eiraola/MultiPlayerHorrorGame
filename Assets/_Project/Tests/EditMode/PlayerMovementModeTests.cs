using NUnit.Framework;
using ProjectScare.Player;
using UnityEngine;

namespace ProjectScare.Tests.EditMode
{
    public class PlayerMovementModeTests
    {
        private const float Tolerance = 0.001f;
        private const float DeltaTime = 0.1f;

        private PlayerMovementConfig config;

        [SetUp]
        public void SetUp() => config = ScriptableObject.CreateInstance<PlayerMovementConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        [Test]
        public void Swim_ForwardFollowsPitch()
        {
            SwimMovement swim = new(config);

            Vector3 lookingUp = swim.Velocity(Vector2.up, 0f, -45f, 0f, false, DeltaTime);
            Vector3 lookingDown = swim.Velocity(Vector2.up, 0f, 45f, 0f, false, DeltaTime);

            Assert.Greater(lookingUp.y, 0f);
            Assert.Less(lookingDown.y, 0f);
            Assert.AreEqual(config.SwimSpeed, lookingUp.magnitude, Tolerance);
        }

        [Test]
        public void Swim_BackIsOppositeOfForward()
        {
            SwimMovement swim = new(config);

            Vector3 forward = swim.Velocity(Vector2.up, 30f, 20f, 0f, false, DeltaTime);
            Vector3 back = swim.Velocity(Vector2.down, 30f, 20f, 0f, false, DeltaTime);

            Assert.AreEqual(0f, (forward + back).magnitude, Tolerance);
        }

        [Test]
        public void Swim_StrafeStaysLevelWhateverThePitch()
        {
            SwimMovement swim = new(config);

            Vector3 velocity = swim.Velocity(Vector2.right, 0f, 60f, 0f, false, DeltaTime);

            Assert.AreEqual(0f, velocity.y, Tolerance);
            Assert.AreEqual(config.SwimSpeed, velocity.x, Tolerance);
        }

        [Test]
        public void Swim_VerticalInputMovesStraightUpWithoutGravity()
        {
            SwimMovement swim = new(config);

            Vector3 rising = swim.Velocity(Vector2.zero, 0f, 0f, 1f, false, DeltaTime);
            Vector3 idle = swim.Velocity(Vector2.zero, 0f, 0f, 0f, false, DeltaTime);

            Assert.AreEqual(new Vector3(0f, config.SwimVerticalSpeed, 0f), rising);
            Assert.AreEqual(Vector3.zero, idle);
        }

        [Test]
        public void Swim_DiagonalIsNotFaster()
        {
            SwimMovement swim = new(config);

            Vector3 velocity = swim.Velocity(Vector2.one, 0f, 0f, 0f, false, DeltaTime);

            Assert.AreEqual(config.SwimSpeed, velocity.magnitude, Tolerance);
        }

        [Test]
        public void Walk_IgnoresPitchAndVerticalInput()
        {
            WalkMovement walk = new(config);
            walk.Enter();

            Vector3 velocity = walk.Velocity(Vector2.up, 0f, -60f, 1f, true, DeltaTime);

            Assert.AreEqual(config.WalkSpeed, velocity.z, Tolerance);
            Assert.AreEqual(config.GroundedVerticalSpeed, velocity.y, Tolerance);
        }

        [Test]
        public void Walk_EnterResetsFalling()
        {
            WalkMovement walk = new(config);
            walk.Velocity(Vector2.zero, 0f, 0f, 0f, false, DeltaTime);

            walk.Enter();
            Vector3 velocity = walk.Velocity(Vector2.zero, 0f, 0f, 0f, false, DeltaTime);

            Assert.AreEqual(config.Gravity * DeltaTime, velocity.y, Tolerance);
        }
    }
}
