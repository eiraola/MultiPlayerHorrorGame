using NUnit.Framework;
using ProjectScare.Core;

namespace ProjectScare.Tests.EditMode
{
    public class JoinGameRequestTests
    {
        private const string SecretPassword = "s3cret-pass";

        [Test]
        public void Create_TrimsAndUppercasesCode()
        {
            JoinGameRequest request = JoinGameRequest.Create("  ab3d \t", null, "Ana");

            Assert.AreEqual("AB3D", request.Code);
        }

        [Test]
        public void Create_NullCode_ReturnsEmptyCode()
        {
            JoinGameRequest request = JoinGameRequest.Create(null, null, "Ana");

            Assert.AreEqual(string.Empty, request.Code);
        }

        [Test]
        public void Create_KeepsPasswordAndNormalizesPlayerName()
        {
            JoinGameRequest request = JoinGameRequest.Create("AB3D", SecretPassword, " Ana ");

            Assert.AreEqual(SecretPassword, request.Password);
            Assert.AreEqual("Ana", request.PlayerName);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Create_UnusedPassword_IsNull(string unused)
        {
            JoinGameRequest request = JoinGameRequest.Create("AB3D", unused, "Ana");

            Assert.IsNull(request.Password);
        }

        [Test]
        public void ToString_DoesNotIncludePassword()
        {
            JoinGameRequest request = JoinGameRequest.Create("AB3D", SecretPassword, "Ana");

            StringAssert.DoesNotContain(SecretPassword, request.ToString());
        }
    }
}
