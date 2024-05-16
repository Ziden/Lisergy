using NUnit.Framework;
using ServerTests;
using Game.Tile;
using GameDataTest;
using Game.Systems.Party;
using Game.Systems.Resources;
using Game.Network.ClientPackets;
using Game.Events.ServerEvents;
using System.Linq;

namespace UnitTests
{
    public class TestResources
    {
        private TestGame _game;
        private PartyEntity _party;
        private TileEntity _logs;

        [SetUp]
        public void Setup()
        {
            _game = new TestGame(createPlayer: false);
            _game.World.Map.GetTile(2, 2).SpecId = TestTiles.FOREST.ID;
        }

        [Test]
        public void TestTileResourcesInitialSetup()
        {
            var world = _game.TestWorld;
            var joinEvent = new JoinWorldPacket();
            var clientPlayer = new TestServerPlayer(_game);
            _game.HandleClientEvent(clientPlayer, joinEvent);
            var party = clientPlayer.GetParty(0);

            foreach (var tilePosition in clientPlayer.VisibilityReferences.VisibleTiles)
            {
                var tile = world.Map.GetTile(tilePosition.X, tilePosition.Y);
                if (tile.Components.Has<TileResourceComponent>())
                {
                    if(!tile.HasHarvestSpot) Assert.Fail($"Tile {tile} has invalid resource ?");
                    var packet = clientPlayer.ReceivedPackets.First(p => p is TileUpdatePacket up && up.Position == tile.Position) as TileUpdatePacket;
                    Assert.That(packet.Components.Any(c => c.GetType() == typeof(TileResourceComponent)));
                }
            }
        }
    }
}