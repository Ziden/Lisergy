using Game;
using Game.Engine.DataTypes;
using Game.Events.ServerEvents;
using Game.Network.ClientPackets;
using Game.Network.ServerPackets;
using Game.Systems.Battle;
using Game.Systems.Battle.Data;
using Game.Systems.Battler;
using Game.Systems.Dungeon;
using Game.Systems.Movement;
using Game.Systems.Party;
using Game.Systems.Tile;
using Game.World;
using NUnit.Framework;
using ServerTests;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests
{
    public class TestDungeon
    {
        private TestGame _game;
        private List<Location> _path;
        private TestServerPlayer _player;
        private PartyEntity _party;
        private DungeonEntity _dungeon;

        [SetUp]
        public void Setup()
        {
            _game = new TestGame();
            _player = _game.GetTestPlayer();
            _path = new List<Location>();
            _party = _player.GetParty(0);
            _dungeon = (DungeonEntity)_game.Entities.CreateEntity(GameId.ZERO, EntityType.Dungeon);
            _dungeon.BuildFromSpec(_game.Specs.Dungeons[0]);
            Assert.That(_dungeon.Get<BattleGroupComponent>().Units.Valids == 1);
            _dungeon.EntityLogic.Map.SetPosition(_game.World.Map.GetTile(8, 8));
        }

        [TearDown]
        public void TearDown()
        {
            // EventPoolValidator.ValidateDisposed();
        }

        [Test]
        public void TestPassiveMoveTowardsDungeon()
        {
            var playerCastleTile = _player.Buildings.First().Tile;
            var dungeonTile = playerCastleTile.GetNeighbor(Direction.EAST);
            var party = _player.GetParty(0);

            // place the dungeon reference
            dungeonTile.Components.GetReference<TileHabitantsReferenceComponent>().Building = _dungeon;

            // send intent to move player to the party
            _player.SendMoveRequest(party, dungeonTile, CourseIntent.Defensive);
            var course = _player.GetParty(0).Course;

            // Complete the move intent
            _game.GameScheduler.ForceComplete(course);

            Assert.AreEqual(dungeonTile, party.Tile);
            Assert.IsTrue(party.Components.Get<BattleGroupComponent>().BattleID.IsZero());
            Assert.AreEqual(0, _player.Data.BattleHeaders.Count());
        }

        [Test]
        public void TestAgressiveMoveOnDungeon()
        {
            var party = _player.GetParty(0);
            var partyTile = party.Tile;
            var dungeonTile = partyTile.GetNeighbor(Direction.EAST);

            _dungeon.EntityLogic.Map.SetPosition(dungeonTile);

            var component = party.Components.Get<BattleGroupComponent>();
            var unit = component.Units[0];
            unit.Atk = 255;
            component.Units[0] = unit;
            party.Save(component);

            _player.SendMoveRequest(party, dungeonTile, CourseIntent.OffensiveTarget);
            var course = party.Course;

            Assert.AreEqual(0, _player.Data.BattleHeaders.Count());

            course.Tick();
            course.Tick();

            var battle = _game.BattleService.GetRunningBattle(party.Components.Get<BattleGroupComponent>().BattleID);
            _game.BattleService.BattleTasks[battle.ID].Tick();

            Assert.AreEqual(dungeonTile, party.Tile);
            Assert.AreEqual(1, _player.Data.BattleHeaders.Count);
        }


        [Test]
        public void TestDungeonComponentsRegisterBuilding()
        {
            var playerCastleTile = _player.Buildings.First().Tile;
            var dungeonTile = playerCastleTile.GetNeighbor(Direction.EAST);

            _dungeon.EntityLogic.Map.SetPosition(dungeonTile);

            var tileBuilding = dungeonTile.Components.GetReference<TileHabitantsReferenceComponent>().Building;
            Assert.That(tileBuilding.EntityId == _dungeon.EntityId);
            Assert.That(tileBuilding.Components.Get<BattleGroupComponent>().Units.Valids > 0);
        }

        [Test]
        public void TestDungeonSpawnsWithMaxHPUnits()
        {
            var team = _dungeon.Components.Get<BattleGroupComponent>().Units;

            var unit = team[0];

            Assert.That(unit.HP == unit.MaxHP && unit.HP > 0);
        }

        [Test]
        public void TestDungeonInitialization()
        {
            Assert.That(_dungeon.Tile != null);
            Assert.That(_dungeon.Get<BattleGroupComponent>().Units.Valids > 0);
        }

        [Test]
        public void TestDungeonRemovedWhenComplete()
        {
            var playerCastleTile = _player.Buildings.First().Tile;
            var dungeonTile = playerCastleTile.GetNeighbor(Direction.EAST);
            var party = _player.GetParty(0);

            var component = party.Components.Get<BattleGroupComponent>();
            var unit = component.Units[0];
            unit.Atk = 255;
            component.Units[0] = unit;
            party.Save(component);

            _dungeon.EntityLogic.Map.SetPosition(dungeonTile);

            _player.SendMoveRequest(party, dungeonTile, CourseIntent.OffensiveTarget);
            _player.GetParty(0).Course.Tick();

            var battle = _game.BattleService.GetRunningBattle(party.Components.Get<BattleGroupComponent>().BattleID);
            _game.BattleService.BattleTasks[battle.ID].Tick();

            Assert.IsTrue(_dungeon.Tile == null);
            Assert.IsTrue(_dungeon.EntityLogic.BattleGroup.IsDestroyed);
            Assert.AreEqual(_dungeon.Tile, null);
            Assert.AreEqual(dungeonTile.Components.GetReference<TileHabitantsReferenceComponent>().Building, null);
            Assert.AreEqual(_player.ReceivedPacketsOfType<EntityDestroyPacket>().Count, 1);
        }

        [Test]
        public void TestPartyTeleportsBackAfterLoosingToDungeon()
        {
            var playerCastleTile = _player.Buildings.First().Tile;
            var dungeonTile = playerCastleTile.GetNeighbor(Direction.EAST);
            var party = _player.GetParty(0);

            var component = _dungeon.Components.Get<BattleGroupComponent>();
            var unit = component.Units[0];
            unit.Atk = 255;
            unit.Speed = 255;
            component.Units[0] = unit;
            _dungeon.Save(component);

            _dungeon.EntityLogic.Map.SetPosition(dungeonTile);

            _player.SendMoveRequest(party, dungeonTile, CourseIntent.OffensiveTarget);
            _player.GetParty(0).Course.Tick();

            var battle = _game.BattleService.GetRunningBattle(party.Components.Get<BattleGroupComponent>().BattleID);
            _game.BattleService.BattleTasks[battle.ID].Tick();

            Assert.IsTrue(party.Tile == playerCastleTile);
            Assert.IsTrue(!party.EntityLogic.BattleGroup.IsDestroyed);
            Assert.AreEqual(_dungeon.Tile, dungeonTile);
            Assert.AreEqual(dungeonTile.Components.GetReference<TileHabitantsReferenceComponent>().Building, _dungeon);
            Assert.AreEqual(_player.ReceivedPacketsOfType<EntityDestroyPacket>().Count, 0);
        }

        [Test]
        public void TestRevealingDungeonReceivesPacket()
        {
            _dungeon = (DungeonEntity)_game.Entities.CreateEntity(GameId.ZERO, EntityType.Dungeon);
            _dungeon.Get<BattleGroupComponent>().Units.Add(new Unit(_game.Specs.Units[1]));
            _dungeon.EntityLogic.Map.SetPosition(_game.World.Map.GetTile(_party.Tile.X + _party.GetLineOfSight() + 1, _party.Tile.Y));

            _game.Entities.DeltaCompression.ClearDeltas();

            var seenClose = _dungeon.Tile.GetNeighbor(Direction.WEST);

            Assert.That(seenClose.EntitiesViewing.Contains(_party));
            Assert.That(!_dungeon.Tile.EntitiesViewing.Contains(_party));

            _player.ReceivedPackets.Clear();
            _party.EntityLogic.Map.SetPosition(_party.Tile.GetNeighbor(Direction.EAST));
            _game.Entities.DeltaCompression.SendDeltaPackets(_player);

            Assert.That(_player.ReceivedPacketsOfType<EntityUpdatePacket>().Any(p => p.Type == EntityType.Dungeon));
            Assert.That(_dungeon.Tile.EntitiesViewing.Contains(_party));
        }

        [Test]
        public void TestDungeonBattleTeamHasUnits()
        {
            _dungeon = (DungeonEntity)_game.Entities.CreateEntity(GameId.ZERO, EntityType.Dungeon);
            _dungeon.Get<BattleGroupComponent>().Units.Add(new Unit(_game.Specs.Units[1]));

            var battleTeam = new BattleTeam(new BattleTeamData(_dungeon));
            Assert.AreNotEqual(battleTeam.Units.Length, 0);
        }

        [Test]
        public void TestReceivingDungeonWhenLogin()
        {
            var joinEvent = new JoinWorldPacket();
            var clientPlayer = new TestServerPlayer(_game);
            _player.ReceivedPackets.Clear();

            var dg = _game.Entities.CreateEntity(GameId.ZERO, EntityType.Dungeon);
            dg.EntityLogic.Map.SetPosition(_game.World.Map.GetTile(4, 2));

            _game.HandleClientEvent(clientPlayer, joinEvent);

            var entityVisibleEvents = clientPlayer.ReceivedPacketsOfType<EntityUpdatePacket>();

            Assert.That(entityVisibleEvents.Any(p => p.Type == EntityType.Dungeon));
            Assert.AreEqual(3, entityVisibleEvents.Count, "Initial Party & Building should be visible");
        }

        [Test]
        public void TestPartyDefeatHealsDungeon()
        {
            var playerCastleTile = _player.Buildings.First().Tile;
            var dungeonTile = playerCastleTile.GetNeighbor(Direction.EAST);
            var party = _player.GetParty(0);

            var component = party.Components.Get<BattleGroupComponent>();
            var unit = component.Units[0];
            unit.Atk = 1;
            unit.HP = 1;
            component.Units[0] = unit;
            party.Save(component);

            dungeonTile.Components.GetReference<TileHabitantsReferenceComponent>().Building = _dungeon;

            _player.SendMoveRequest(_player.GetParty(0), dungeonTile, CourseIntent.OffensiveTarget);
            _player.GetParty(0).Course.Tick();

            var b = _game.BattleService.GetRunningBattle(party.Get<BattleGroupComponent>().BattleID);
            _game.BattleService.BattleTasks[b.ID].Tick();
        }
    }
}