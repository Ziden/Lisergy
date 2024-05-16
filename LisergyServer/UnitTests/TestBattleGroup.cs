using Game.Systems.Battler;
using Game.Systems.Party;
using NUnit.Framework;
using ServerTests;

namespace UnitTests
{
    public class TestBattleGroup
    {
        private TestGame Game;

        [SetUp]
        public void Setup()
        {
            Game = new TestGame();
        }

        [Test]
        public void TestAddSingleUnit()
        {
            var e = new PartyEntity(Game, Game.CreatePlayer().EntityId);
            var startUnits = e.Get<BattleGroupComponent>().Units.Valids;

            e.EntityLogic.BattleGroup.AddUnit(new Unit(Game.Specs.Units[1]));

            Assert.AreEqual(startUnits + 1, e.Get<BattleGroupComponent>().Units.Valids);
        }
    }
}
