using Game.Engine.DataTypes;
using Game.Systems.Battle;
using Game.Systems.Battle.BattleActions;
using Game.Systems.Battle.Data;
using Game.Systems.Battler;
using GameDataTest;
using NUnit.Framework;
using System;
using System.Linq;

namespace UnitTests
{
    public unsafe class TestBattleActions
    {
        private Unit FastUnit;
        private Unit SlowUnit;
        private TurnBattle Battle;

        [SetUp]
        public void Setup()
        {
            var specs = TestSpecs.Generate();

            FastUnit = new Unit(specs.Units[1]);
            FastUnit.Speed *= 2;

            SlowUnit = new Unit(specs.Units[1]);
            SlowUnit.Speed /= 2;

            Battle = new TurnBattle(GameId.Generate(), new BattleTeamData(FastUnit), new BattleTeamData(SlowUnit));
        }

        [Test]
        public void TestAttackAction()
        {
            var attacker = Battle.CurrentActingUnit;
            var defender = Battle.GetOpposingTeam(attacker).Units.First();

            var action = new AttackAction(Battle, attacker, defender);
            Battle.ReceiveAction(action);

            var result = action.Result as AttackActionResult;

            Assert.NotNull(result);
            Assert.That(defender.UnitPtr->HP == defender.UnitPtr->MaxHP - result.Damage);
        }
    }
}