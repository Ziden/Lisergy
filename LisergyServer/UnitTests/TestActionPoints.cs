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
    public class TestActionPoints
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
        }

        private void SendMoveRequest()
        {
            var ev = new MoveRequestPacket() { Path = _path, PartyIndex = _party.PartyIndex };
            ev.Sender = _player;
            _game.HandleClientEvent(_player, ev);
        }

        [Test]
        public void TestCourseNoActionPoint()
        {
            var tile = _party.Tile;
            var next = tile.GetNeighbor(Direction.SOUTH);
            _path.Add(new Location(next.X, next.Y));

            SendMoveRequest();

            Assert.AreEqual(0, _game.GameScheduler.PendingTasks);
        }
        
        [Test]
        public void TestCourseAfterActionPoint()
        {
            var tile = _party.Tile;
            var next = tile.GetNeighbor(Direction.SOUTH);
            _path.Add(new Location(next.X, next.Y));

            _party.EntityLogic.ActionPoints.SetActionPoints(1);
            SendMoveRequest();

            Assert.AreEqual(1, _game.GameScheduler.PendingTasks);
            Assert.IsTrue(_party.Course != null);
        }
        
        [Test]
        public void TestActionPointDeducted()
        {
            var tile = _party.Tile;
            var next = tile.GetNeighbor(Direction.SOUTH);
            _path.Add(new Location(next.X, next.Y));

            _party.EntityLogic.ActionPoints.SetActionPoints(1);
            SendMoveRequest();

            Assert.AreEqual(0, _party.EntityLogic.ActionPoints.GetActionPoints());
        }
        
        [Test]
        public void TestTakingTurnIncreaseActionPoint()
        {
            Assert.AreEqual(0, _party.EntityLogic.ActionPoints.GetActionPoints());
            
            _player.EntityLogic.Player.TakeTurn();
            
            Assert.AreEqual(1, _party.EntityLogic.ActionPoints.GetActionPoints());
        }
    }
}