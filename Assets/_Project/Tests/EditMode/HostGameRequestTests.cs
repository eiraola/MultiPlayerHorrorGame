using NUnit.Framework;
using ProjectScare.Core;

namespace ProjectScare.Tests.EditMode
{
    public class HostGameRequestTests
    {
        private const string SecretPassword = "s3cret-pass";

        [Test]
        public void Create_TrimsName()
        {
            HostGameRequest request = CreateRequest("  Night shift \t");

            Assert.AreEqual("Night shift", request.Name);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Create_KeepsVisibility(bool isPublic)
        {
            HostGameRequest request = HostGameRequest.Create("Game", isPublic, null, null, "Ana");

            Assert.AreEqual(isPublic, request.IsPublic);
        }

        [Test]
        public void Create_NullName_ReturnsEmptyName()
        {
            HostGameRequest request = CreateRequest(null);

            Assert.AreEqual(string.Empty, request.Name);
        }

        [Test]
        public void Create_KeepsRegionPasswordAndPlayerName()
        {
            HostGameRequest request = HostGameRequest.Create("Game", true, "europe-west4", SecretPassword, " Ana ");

            Assert.AreEqual("europe-west4", request.Region);
            Assert.AreEqual(SecretPassword, request.Password);
            Assert.AreEqual("Ana", request.PlayerName);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Create_UnusedRegionAndPassword_AreNull(string unused)
        {
            HostGameRequest request = HostGameRequest.Create("Game", true, unused, unused, "Ana");

            Assert.IsNull(request.Region);
            Assert.IsNull(request.Password);
        }

        [TestCase(8)]
        [TestCase(64)]
        public void IsValidPassword_WithinLimits_ReturnsTrue(int length)
        {
            Assert.IsTrue(HostGameRequest.IsValidPassword(new string('x', length)));
        }

        [TestCase(0)]
        [TestCase(7)]
        [TestCase(65)]
        public void IsValidPassword_OutsideLimits_ReturnsFalse(int length)
        {
            Assert.IsFalse(HostGameRequest.IsValidPassword(new string('x', length)));
        }

        [Test]
        public void IsValidPassword_Null_ReturnsFalse()
        {
            Assert.IsFalse(HostGameRequest.IsValidPassword(null));
        }

        [Test]
        public void ToString_DoesNotIncludePassword()
        {
            HostGameRequest request = HostGameRequest.Create("Game", true, null, SecretPassword, "Ana");

            StringAssert.DoesNotContain(SecretPassword, request.ToString());
        }

        private static HostGameRequest CreateRequest(string rawName) =>
            HostGameRequest.Create(rawName, false, null, null, "Ana");
    }
}
