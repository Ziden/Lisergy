using Game.Engine.DataTypes;
using Game.Events.ServerEvents;
using Game.Network.ClientPackets;
using Game.Systems.Battler;
using Game.Systems.FogOfWar;
using Game.Systems.Map;
using Game.Systems.Movement;
using Game.Systems.Party;
using Game.World;
using NUnit.Framework;
using ServerTests;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests
{
    public class TestMovement
    {
        private TestGame _game;
        private List<Location> _path;
        private TestServerPlayer _player;
        private PartyEntity _party;

        [SetUp]
        public void Setup()
        {
            _game = new TestGame();
            _player = _game.GetTestPlayer();
            _path = new List<Location>();
            _party = _player.GetParty(0);
            _party.EntityLogic.ActionPoints.SetActionPoints(255);
        }

        private void SendMoveRequest()
        {
            _party.EntityLogic.ActionPoints.SetActionPoints((byte)_path.Count);
            var ev = new MoveRequestPacket() { Path = _path, PartyIndex = _party.PartyIndex };
            ev.Sender = _player;
            _game.HandleClientEvent(_player, ev);
        }

        [Test]
        public void TestSettingCourseTask()
        {
            var tile = _party.Tile;
            var next = tile.GetNeighbor(Direction.SOUTH);
            _path.Add(new Location(next.X, next.Y));

            SendMoveRequest();

            Assert.AreEqual(1, _game.GameScheduler.PendingTasks);
            Assert.IsTrue(_party.Course != null);
        }

        [Test]
        public void TestCourseMovingParty()
        {
            var date = DateTime.MinValue;
            _game.GameScheduler.SetLogicalTime(date);

            var tile = _party.Tile;
            var next = tile.GetNeighbor(Direction.SOUTH);
            _path.Add(new Location(next.X, next.Y));

            _player.ListenTo<EntityMoveInEvent>();

            SendMoveRequest();

            Assert.AreEqual(tile, _party.Tile);

            _game.GameScheduler.Tick(date + _party.Course.Delay);

            Assert.AreEqual(next, _party.Tile);
        }

        /// <summary>
        /// When moving outside of a player vision, the player should still receive an entity update
        /// so he is aware this entity moved out. He would be aware by noticing the position component
        /// </summary>
        [Test]
        public void TestMoveOutsidePlayerVision()
        {
            var tile = _party.Tile;
            var visibleTiles = _party.Tile.GetAOE(_party.Get<EntityVisionComponent>().LineOfSight);
            var next = tile.GetNeighbor(Direction.SOUTH);

            var player2 = _game.CreatePlayer(5, 7);
            var player2Party = player2.GetParty(0); // placed in 8 - 7

            Assert.That(player2Party.Tile.PlayersViewing.Contains(_player));

            _game.Entities.DeltaCompression.ClearDeltas();
            _player.ReceivedPackets.Clear();

            // Moving player2 to 5 7 which is slightly outside p1 vision
            player2.EntityLogic.Player.TakeTurn();
            _path.Add(new Location(5, 7));
            _game.HandleClientEvent(player2, new MoveRequestPacket() { Path = _path, PartyIndex = player2Party.PartyIndex });
            _game.GameScheduler.Tick(_game.GameScheduler.Now + player2Party.Course.Delay);

            // p1 should still have received p2 component update even tho the entity is outside his vision because it was inside vision
            var moveEvents = _player.ReceivedPacketsOfType<EntityUpdatePacket>().Where(p => p.EntityId == player2Party.EntityId && p.SyncedComponents.Any(c => c is MapPlacementComponent));

            // should have received movement events
            Assert.AreEqual(1, moveEvents.Count());
        }

        [Test]
        public void TestMoveEvents()
        {
            var tile = _party.Tile;
            var next = tile.GetNeighbor(Direction.SOUTH);
            _path.Add(new Location(next.X, next.Y));

            _game.Entities.DeltaCompression.ClearDeltas();
            _player.ReceivedPackets.Clear();

            SendMoveRequest();
            _game.GameScheduler.Tick(_game.GameScheduler.Now + _party.Course.Delay);

            var moveEvents = _player.ReceivedPacketsOfType<EntityUpdatePacket>().Where(p => p.EntityId == _party.EntityId && p.SyncedComponents.Any(c => c is MapPlacementComponent));
            var tileDiscovery = _player.ReceivedPacketsOfType<TileUpdatePacket>();

            // should have received movement events
            Assert.AreEqual(1, moveEvents.Count());
        }

        [Test]
        public void TestCourseTask()
        {
            var tile = _party.Tile;
            var next = tile.GetNeighbor(Direction.SOUTH);
            var party = _player.GetParty(0);

            _player.SendMoveRequest(party, next, CourseIntent.OffensiveTarget);
            var course = party.Course;

            course.Tick();
            course.Tick();

            Assert.AreEqual(party.Tile, next);
        }

        [Test]
        public void TestChangingCourse()
        {
            var tile = _party.Tile;
            var next = tile.GetNeighbor(Direction.SOUTH);
            _path.Add(new Location(next.X, next.Y));

            SendMoveRequest();
            var course1 = _game.GameScheduler.Queue.First();

            _path.Add(new Location(next.X + 1, next.Y));
            SendMoveRequest();
            var course2 = _game.GameScheduler.Queue.First();

            Assert.AreNotEqual(course1, course2);
            Assert.IsFalse(_game.GameScheduler.Queue.Contains(course1));
            Assert.IsTrue(_game.GameScheduler.Queue.Contains(course2));
            Assert.IsFalse(course2.HasFinished);
        }

        [Test]
        public void TestCannotMoveBattlingUnit()
        {
            var tile = _party.Tile;
            var next = tile.GetNeighbor(Direction.SOUTH);
            _path.Add(new Location(next.X, next.Y));
            var component = _party.Get<BattleGroupComponent>();
            component.BattleID = GameId.Generate();
            _party.Save(component);

            _path.Add(new Location(next.X + 1, next.Y));
            SendMoveRequest();

            Assert.AreEqual(GameId.ZERO, _party.Get<CourseComponent>().CourseId);
        }

        [Test]
        public void TestMultipleMoves()
        {
            var date = DateTime.MinValue;
            _game.GameScheduler.SetLogicalTime(date);

            var tile = _party.Tile;
            var next1 = tile.GetNeighbor(Direction.SOUTH);
            var next2 = next1.GetNeighbor(Direction.SOUTH);
            var next3 = next2.GetNeighbor(Direction.SOUTH);
            _path.Add(new Location(next1.X, next1.Y));
            _path.Add(new Location(next2.X, next2.Y));
            _path.Add(new Location(next3.X, next3.Y));

            _game.Entities.DeltaCompression.ClearDeltas();
            _player.ReceivedPackets.Clear();

            SendMoveRequest();

            Assert.AreEqual(tile, _party.Tile);

            date += _party.Course.Delay;
            _game.GameScheduler.Tick(date);
            Assert.AreEqual(next1, _party.Tile);

            date += _party.Course.Delay;
            _game.GameScheduler.Tick(date);
            Assert.AreEqual(next2, _party.Tile);

            date += _party.Course.Delay;
            _game.GameScheduler.Tick(date);
            Assert.AreEqual(next3, _party.Tile);

            var moveEvents = _player.ReceivedPacketsOfType<EntityUpdatePacket>().Where(p => p.EntityId == _party.EntityId && p.SyncedComponents.Any(c => c is MapPlacementComponent));
            Assert.AreEqual(3, moveEvents.Count());
        }
    }
}