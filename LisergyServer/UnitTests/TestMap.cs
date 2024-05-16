using Game;
using Game.Events.ServerEvents;
using Game.Network.ClientPackets;
using Game.Network.ServerPackets;
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
    public class TestMap
    {
        [Test]
        public void TestMapCreation()
        {
            var game = new TestGame(createWorld: false);
            var world = new GameWorld(game, 8, 8);
            game.SetupWorld(world);
            var player = game.CreatePlayer(4, 4);

            Assert.AreEqual(player, world.Players[player.EntityId]);
        }
    }
}