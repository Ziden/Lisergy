using Game;
using Game.Events.ServerEvents;
using NUnit.Framework;
using ServerTests;
using System;
using System.Linq;

namespace UnitTests
{

    [SetUpFixture]
    public class TestFixture : IDisposable
    {
        public TestFixture()
        {
            // runs before all tests
        }

        public void Dispose()
        {
            UnmanagedMemory.FlagMemoryToBeReused();
        }
    }

    public class TestWorldEvents
    {
        private TestGame game;

        [Test]
        public void TestSendingTestPlayerEvents()
        {
            game = new TestGame();
            var player = game.GetTestPlayer();

            var events = game.SentServerPackets
                .Where(e => e is TileUpdatePacket)
                .Select(e => (TileUpdatePacket)e)
                .ToList();
        }
    }
}