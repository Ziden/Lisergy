using NUnit.Framework;
using ServerTests;
using Game.Systems.Player;
using Game.Tile;
using Game.Systems.Tile;
using GameDataTest;
using Game.Systems.Party;
using Game.World;
using Game.Systems.Resources;

namespace UnitTests
{
    public class TestCargo
    {
        private TestGame _game;
        private PlayerEntity _player;
        private PartyEntity _party;

        [SetUp]
        public void Setup()
        {
            _game = new TestGame();
            _player = _game.GetTestPlayer();
            _party = _player.GetParty(0);
        }

        [Test]
        public void TestAddToCargo()
        {
            _party.EntityLogic.Cargo.AddTocargo(new ResourceStackData(0, 10));

            var cargo = _party.Get<CargoComponent>();

            Assert.AreEqual(10, cargo.GetAmount(0));
            Assert.IsTrue(cargo.CurrentWeight > 0);
        }
    }
}