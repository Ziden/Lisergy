using Game.Engine;
using Game.Events.ServerEvents;
using NUnit.Framework;
using ServerTests;

namespace UnitTests
{
    public class TestSpecSerialization
    { 
        [Test]
        public void TestBasicSerialization()
        {
            var game = new TestGame();
            var serialized = Serialization.FromBasePacket(new GameSpecPacket(game));
            var deserialized = (GameSpecPacket)Serialization.ToBasePacket(serialized);

            Assert.AreEqual(game.Specs.Units.Count, deserialized.Spec.Units.Count);
        }
    }
}