using NUnit.Framework;
using ProjectScare.Core;

namespace ProjectScare.Tests.EditMode
{
    public class RoomSummaryTests
    {
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, true)]
        public void IsFull_ComparesPlayersWithMaximum(int players, bool expected)
        {
            var room = new RoomSummary("id", "Room", "Ana", "europe-west4", players, 2, false);

            Assert.AreEqual(expected, room.IsFull);
        }
    }
}
