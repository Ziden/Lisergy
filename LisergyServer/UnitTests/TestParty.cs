using Game;
using Game.Engine;
using Game.Engine.DataTypes;
using Game.Events.ServerEvents;
using Game.Network.ServerPackets;
using Game.Systems.Battler;
using Game.Systems.Dungeon;
using Game.Systems.Party;
using NUnit.Framework;
using ServerTests;
using System;
using System.Linq;

namespace UnitTests
{
    public class TestParty
    {
        private TestGame _game;
        private TestServerPlayer _player;

        [SetUp]
        public void Setup()
        {
            _game = new TestGame();
            _player = _game.GetTestPlayer();
        }

        [Test]
        public void TestPartyStatusUpdatedAfterBattle()
        {
            var playerCastleTile = _player.Buildings.First().Tile;
            var party = _player.GetParty(0);
            _game.Systems.Map.GetLogic(party).SetPosition(_game.World.Map.GetTile(0, 0));

            var enemy = _game.Entities.CreateEntity(GameId.ZERO, EntityType.Dungeon);
            _game.Systems.Map.GetLogic(enemy).SetPosition(_game.World.Map.GetTile(1, 1));

            enemy.Get<BattleGroupComponent>().Units.Add(new Unit(_game.Specs.Units[0]));

            var battleID = GameId.Generate();
            _game.Network.SendToServer(new BattleQueuedPacket(battleID, party, enemy), ServerType.BATTLE);

            _player.ReceivedPackets.Clear();
            _game.Entities.DeltaCompression.ClearDeltas();

            var battle = _game.BattleService.GetRunningBattle(battleID);
            _game.BattleService.BattleTasks[battle.ID].Tick();

            var statusUpdates = _player.ReceivedPacketsOfType<EntityUpdatePacket>().Where(p => p.Type == EntityType.Party);
            Assert.AreEqual(1, statusUpdates.Count());
        }


        [Test]
        public void TestPartyUnitsInitialHP()
        {
            var team = _player.GetParty(0).Get<BattleGroupComponent>().Units;

            var unit = team[0];
            var mhp = unit.MaxHP;

            Assert.That(unit.HP == unit.MaxHP && unit.HP > 0);
        }

        [Test]
        public void TestBattleIDResetsAfterBattle()
        {
            var party = _player.GetParty(0);
            party.EntityLogic.Map.SetPosition(_game.World.Map.GetTile(0, 0));

        
            var enemy = _game.Entities.CreateEntity(GameId.ZERO, EntityType.Dungeon);
            enemy.EntityLogic.Map.SetPosition(_game.World.Map.GetTile(0, 0));

            var component = enemy.Get<BattleGroupComponent>();
            component.Units.Add(new Unit(_game.Specs.Units[0]));
            enemy.Save(component);

            var battleID = GameId.Generate();
            _game.Network.SendToServer(new BattleQueuedPacket(battleID, party, enemy), ServerType.BATTLE);

            var battle = _game.BattleService.GetRunningBattle(battleID);
            _game.BattleService.BattleTasks[battle.ID].Tick();

            Assert.IsTrue(party.Get<BattleGroupComponent>().BattleID == GameId.ZERO);
            Assert.IsTrue(battle.IsOver);
        }

        [Test]
        public void TestPartyOwner()
        {
            var party = _player.GetParty(0);

            Assert.AreEqual(party.OwnerID, _player.EntityId);
        }

        [Test]
        public void TestPartyReplaceUnits()
        {
            var unit1 = new Unit(_game.Specs.Units[0]);
            var unit2 = new Unit(_game.Specs.Units[1]);
            var unit3 = new Unit(_game.Specs.Units[2]);

            var party = _game.Entities.CreateEntity(_player.EntityId, EntityType.Party);
            var logic = _game.EntityLogic(party).BattleGroup;
            logic.AddUnit(unit1);
            logic.AddUnit(unit2);

            logic.ReplaceUnit(unit1, unit3);
   
            Assert.AreEqual(2, party.Get<BattleGroupComponent>().Units.Valids);
            Assert.IsTrue(party.Get<BattleGroupComponent>().Units.Contains(unit3));
            Assert.IsTrue(party.Get<BattleGroupComponent>().Units.Contains(unit2));
            Assert.IsFalse(party.Get<BattleGroupComponent>().Units.Contains(unit1));
        }

        [Test]
        public void TestReplaceAtIndexLogic()
        {
            var unit1 = new Unit(_game.Specs.Units[0]);
            var unit2 = new Unit(_game.Specs.Units[0]);
            var unit3 = new Unit(_game.Specs.Units[2]);

            var party = _game.Entities.CreateEntity(GameId.ZERO, EntityType.Dungeon);
            _game.EntityLogic(party).BattleGroup.AddUnit(unit1);
            _game.EntityLogic(party).BattleGroup.AddUnit(unit2);
            _game.EntityLogic(party).BattleGroup.ReplaceUnit(unit2, unit3, 1);

            Assert.AreEqual(2, party.Get<BattleGroupComponent>().Units.Valids);
        }

        [Test]
        public void TestPartyUpdateUpdates()
        {
            var unit0 = new Unit(_game.Specs.Units[0]);
            var unit1 = new Unit(_game.Specs.Units[1]);
            var unit2 = new Unit(_game.Specs.Units[2]);

            var party = _game.Entities.CreateEntity(_player.EntityId, EntityType.Party);
            var logic = _game.EntityLogic(party).BattleGroup;
            logic.AddUnit(unit0);
            logic.AddUnit(unit1);
            logic.AddUnit(unit0);
            logic.AddUnit(unit1);

            var newUnits = new Unit [] { unit1, unit2, unit0, default };

            logic.UpdateUnits(newUnits);

            var unitGroup = party.Get<BattleGroupComponent>().Units;
            Assert.AreEqual(3, unitGroup.Valids);
            Assert.AreEqual(unitGroup[0], unit1);
            Assert.AreEqual(unitGroup[1], unit2);
            Assert.AreEqual(unitGroup[2], unit0);
 
        }

        [Test]
        public void TestPartyBattleUnitsSerialized()
        {
            var party = _player.GetParty(0);

            // Trigger a delta
            party.Components.Save(party.Components.Get<BattleGroupComponent>());

            var update = party.GetUpdatePacket(_player);

            var serialize = Serialization.FromBasePacket(update);
            var deserialize = Serialization.ToPacket<EntityUpdatePacket>(serialize);

            var unitsComponent = (BattleGroupComponent)deserialize.SyncedComponents.FirstOrDefault(c => c.GetType() == typeof(BattleGroupComponent));

            var u1 = unitsComponent.Units[0];
            var u2 = party.Get<BattleGroupComponent>().Units[0];
            Assert.IsTrue(u1 == u2);
        }

        private void UpdateParty(ref PartyComponent c)
        {

        }

        [Test]
        public void TestPartyDoesNotReceiveDestroyPacket()
        {
            var party = _player.GetParty(0);
            var component = party.Get<PartyComponent>();
            component.PartyIndex = 2;
            party.Save(component);

            var update = party.GetUpdatePacket(_player);

            var serialize = Serialization.FromBasePacket(update);
            var deserialize = Serialization.ToPacket<EntityUpdatePacket>(serialize);

            Assert.IsTrue(deserialize.SyncedComponents.Any(c => c is PartyComponent));
            Assert.AreEqual(2, ((PartyComponent)deserialize.SyncedComponents.First(c => c is PartyComponent)).PartyIndex);

        }
    }
}