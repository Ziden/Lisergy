using Game.Engine;
using Game.Engine.DataTypes;
using Game.Events;
using Game.Network.ServerPackets;
using Game.Systems.Battle;
using Game.Systems.Battle.BattleActions;
using Game.Systems.Battle.Data;
using Game.Systems.Battler;
using GameDataTest;
using NUnit.Framework;
using ServerTests;
using System;
using System.Linq;

namespace UnitTests
{
    public unsafe class TestAutobattles
    {
        private Unit StrongUnit;
        private Unit WeakUnit;
        private Unit FastUnit;
        private Unit SlowUnit;

        [SetUp]
        public void Setup()
        {
            var specs = TestSpecs.Generate();
            StrongUnit = new Unit(specs.Units[1]);
            StrongUnit.Atk *= 4;

            WeakUnit = new Unit(specs.Units[1]);

            FastUnit = new Unit(specs.Units[1]);
            FastUnit.Speed *= 10;

            SlowUnit = new Unit(specs.Units[1]);
            SlowUnit.Speed /= 2;
        }

        [Test]
        public void TestUnitsOrderingSameSpeed()
        {
            var battle = new TestBattle(new BattleTeamData(StrongUnit), new BattleTeamData(WeakUnit));
            var first = battle.NextUnitToAct;

            Assert.AreEqual(first.RT, first.MaxRT);

            battle.AutoRun.RunOneTurn();

            var second = battle.NextUnitToAct;

            Assert.AreNotEqual(first, second);
            Assert.AreEqual(first.MaxRT * 2, first.RT);
            Assert.AreEqual(second.MaxRT, second.RT);
        }

        [Test]
        public void TestFasterActFirst()
        {
            var battle = new TestBattle(new BattleTeamData(WeakUnit), new BattleTeamData(FastUnit));

            Assert.AreEqual(battle.NextUnitToAct.UnitID, FastUnit.Id);

            var lastAction = battle.AutoRun.RunOneTurn().Last() as BattleAction;

            Assert.AreEqual(lastAction.Unit.UnitID, FastUnit.Id);
            Assert.AreEqual(lastAction.Unit.RT, lastAction.Unit.MaxRT * 2);
        }

        [Test]
        public void TestUnitDelay()
        {
            var battle = new TestBattle(new BattleTeamData(FastUnit), new BattleTeamData(WeakUnit));
            var result = battle.AutoRun.RunAllRounds();

            var fastAttacks = result.Turns.Where(r => r.Events.Any(a => a is BattleAction && ((BattleAction)a).Unit.UnitID == FastUnit.Id)).ToList();
            var weakAttacks = result.Turns.Where(r => r.Events.Any(a => a is BattleAction && ((BattleAction)a).Unit.UnitID == WeakUnit.Id)).ToList();

            Assert.That(fastAttacks.Count() > weakAttacks.Count());
        }

        [Test]
        public void TestDelayProportion()
        {
            FastUnit.HP = 60;

            SlowUnit.HP = 60;

            var battle = new TestBattle(new BattleTeamData(FastUnit), new BattleTeamData(SlowUnit));
            var result = battle.AutoRun.RunAllRounds();

            var fastAttacks = result.Turns.Where(r => r.Events.Any(a => a is BattleAction && ((BattleAction)a).Unit.UnitID == FastUnit.Id)).ToList();
            var slowAttacks = result.Turns.Where(r => r.Events.Any(a => a is BattleAction && ((BattleAction)a).Unit.UnitID == SlowUnit.Id)).ToList();

            Assert.That(fastAttacks.Count() > slowAttacks.Count());
        }

        [Test]
        public void TestWinner()
        {
            var battle = new TurnBattle(GameId.Generate(), new BattleTeamData(StrongUnit), new BattleTeamData(WeakUnit));
            var result = battle.AutoRun.RunAllRounds();

            Assert.AreEqual(result.Winner, result.Attacker);
        }


        [Test]
        public void TestUnitsBeingUpdated()
        {
            var initialHP = StrongUnit.HP;
            var battle = new TurnBattle(GameId.Generate(), new BattleTeamData(StrongUnit), new BattleTeamData(WeakUnit));
            var result = battle.AutoRun.RunAllRounds();

            var finalHP = result.Attacker.Units[0].UnitPtr->HP;
            Assert.AreNotEqual(initialHP, finalHP);
        }

        [Test]
        public void TestSerialization()
        {
            Serialization.LoadSerializers();
            var battle = new TurnBattle(GameId.Generate(), new BattleTeamData(StrongUnit), new BattleTeamData(WeakUnit));
            var result = battle.AutoRun.RunAllRounds();

            var ev = new BattleResultPacket(battle.ID, result);

            var bytes = Serialization.FromPacket(ev);
            ev = Serialization.ToPacket<BattleResultPacket>(bytes);

            Assert.AreEqual(ev.Header.Attacker.Units[0].Id, result.Attacker.Units.First().UnitID);
        }
    }
}