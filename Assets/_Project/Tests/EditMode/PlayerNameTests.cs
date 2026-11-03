using NUnit.Framework;
using ProjectScare.Core;

namespace ProjectScare.Tests.EditMode
{
    public class PlayerNameTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(" \t\n")]
        public void IsValid_WithoutVisibleCharacters_ReturnsFalse(string rawName)
        {
            Assert.IsFalse(PlayerName.IsValid(rawName));
        }

        [TestCase("Ana")]
        [TestCase("  Ana  ")]
        public void IsValid_WithVisibleCharacters_ReturnsTrue(string rawName)
        {
            Assert.IsTrue(PlayerName.IsValid(rawName));
        }

        [Test]
        public void Normalize_TrimsSurroundingWhitespace()
        {
            Assert.AreEqual("Ana Maria", PlayerName.Normalize("  Ana Maria \t"));
        }

        [Test]
        public void Normalize_Null_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, PlayerName.Normalize(null));
        }
    }
}
