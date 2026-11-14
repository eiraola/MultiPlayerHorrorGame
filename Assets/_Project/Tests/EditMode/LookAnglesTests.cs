using NUnit.Framework;
using ProjectScare.Player;
using UnityEngine;

namespace ProjectScare.Tests.EditMode
{
    public class LookAnglesTests
    {
        private const float Sensitivity = 0.1f;
        private const float MinPitch = -80f;
        private const float MaxPitch = 80f;

        private static LookAngles Apply(LookAngles angles, float x, float y) =>
            angles.Apply(new Vector2(x, y), Sensitivity, MinPitch, MaxPitch);

        [Test]
        public void MouseRight_TurnsRight()
        {
            LookAngles result = Apply(new LookAngles(10f, 0f), 100f, 0f);

            Assert.AreEqual(20f, result.Yaw, 0.001f);
            Assert.AreEqual(0f, result.Pitch, 0.001f);
        }

        [Test]
        public void MouseUp_LooksUp()
        {
            LookAngles result = Apply(new LookAngles(0f, 0f), 0f, 100f);

            Assert.AreEqual(-10f, result.Pitch, 0.001f);
        }

        [TestCase(10000f, MinPitch)]
        [TestCase(-10000f, MaxPitch)]
        public void Pitch_NeverLeavesTheLimits(float mouseY, float expected)
        {
            LookAngles result = Apply(new LookAngles(0f, 0f), 0f, mouseY);

            Assert.AreEqual(expected, result.Pitch, 0.001f);
        }

        [TestCase(350f, 200f, 10f)]
        [TestCase(10f, -200f, 350f)]
        public void Yaw_WrapsAroundAFullTurn(float startYaw, float mouseX, float expected)
        {
            LookAngles result = Apply(new LookAngles(startYaw, 0f), mouseX, 0f);

            Assert.AreEqual(expected, result.Yaw, 0.001f);
        }
    }
}
