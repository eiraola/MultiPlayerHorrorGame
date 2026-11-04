using NUnit.Framework;
using ProjectScare.Core;

namespace ProjectScare.Tests.EditMode
{
    public class LobbyRulesTests
    {
        private static LobbyPlayerEntry[] Players(params bool[] readyStates)
        {
            var players = new LobbyPlayerEntry[readyStates.Length];

            for (int i = 0; i < players.Length; i++)
                players[i] = new LobbyPlayerEntry($"Player {i}", readyStates[i]);

            return players;
        }

        [Test]
        public void AllReady_IsFalseWithoutPlayers()
        {
            Assert.IsFalse(LobbyRules.AllReady(null));
            Assert.IsFalse(LobbyRules.AllReady(Players()));
        }

        [TestCase(true, ExpectedResult = true)]
        [TestCase(false, ExpectedResult = false)]
        public bool AllReady_WithOnePlayer(bool ready) => LobbyRules.AllReady(Players(ready));

        [TestCase(true, true, ExpectedResult = true)]
        [TestCase(true, false, ExpectedResult = false)]
        [TestCase(false, true, ExpectedResult = false)]
        [TestCase(false, false, ExpectedResult = false)]
        public bool AllReady_WithTwoPlayers(bool first, bool second) => LobbyRules.AllReady(Players(first, second));

        [TestCase(true, true, true, true, ExpectedResult = true)]
        [TestCase(false, true, true, true, ExpectedResult = false)]
        [TestCase(true, true, false, true, ExpectedResult = false)]
        [TestCase(true, true, true, false, ExpectedResult = false)]
        public bool AllReady_WithFourPlayers(bool first, bool second, bool third, bool fourth) =>
            LobbyRules.AllReady(Players(first, second, third, fourth));

        [TestCase(LobbyRules.NoCountdown, ExpectedResult = true)]
        [TestCase(LobbyRules.CountdownStart, ExpectedResult = false)]
        [TestCase(1, ExpectedResult = false)]
        [TestCase(LobbyRules.CountdownFinished, ExpectedResult = false)]
        public bool CanPressStart_OnlyWithoutCountdown(int countdown) =>
            LobbyRules.CanPressStart(Players(true, true), countdown);

        [Test]
        public void CanPressStart_RequiresEveryoneReady()
        {
            Assert.IsTrue(LobbyRules.CanPressStart(Players(true), LobbyRules.NoCountdown));
            Assert.IsFalse(LobbyRules.CanPressStart(Players(true, false), LobbyRules.NoCountdown));
            Assert.IsFalse(LobbyRules.CanPressStart(Players(true, true, true, false), LobbyRules.NoCountdown));
            Assert.IsFalse(LobbyRules.CanPressStart(Players(), LobbyRules.NoCountdown));
        }

        [TestCase(1, 1, ExpectedResult = true)]
        [TestCase(2, 2, ExpectedResult = true)]
        [TestCase(4, 4, ExpectedResult = true)]
        [TestCase(1, 2, ExpectedResult = false)]
        [TestCase(3, 4, ExpectedResult = false)]
        public bool IsEveryoneRegistered_ComparesPlayersWithConnections(int playerCount, int connectedCount) =>
            LobbyRules.IsEveryoneRegistered(playerCount, connectedCount);
    }
}
