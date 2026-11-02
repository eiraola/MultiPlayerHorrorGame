using NUnit.Framework;
using ProjectScare.Core;

namespace ProjectScare.Tests.EditMode
{
    public class HostGameRequestTests
    {
        [Test]
        public void Create_TrimsName()
        {
            HostGameRequest request = HostGameRequest.Create("  Night shift \t", true);

            Assert.AreEqual("Night shift", request.Name);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Create_KeepsVisibility(bool isPublic)
        {
            HostGameRequest request = HostGameRequest.Create("Game", isPublic);

            Assert.AreEqual(isPublic, request.IsPublic);
        }

        [Test]
        public void Create_NullName_ReturnsEmptyName()
        {
            HostGameRequest request = HostGameRequest.Create(null, false);

            Assert.AreEqual(string.Empty, request.Name);
        }
    }
}
