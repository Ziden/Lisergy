using Game;
using Game.Engine;
using Game.Engine.ECS;
using Game.Events.ServerEvents;
using Game.Systems.FogOfWar;
using Game.Systems.Map;
using Game.Systems.Movement;
using Game.Systems.Party;
using Game.Systems.Resources;
using Game.Systems.Tile;
using Game.World;
using NUnit.Framework;
using ServerTests.Integration.Stubs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telepathy;

namespace ServerTests.Integration
{
    /// <summary>
    /// This test will run server in a thread and client in another thread and do actual networking using local network.
    /// This is a single test that should cover the main use case of the client game SDK end to end
    /// This is done as a sdk instead of a client so it's easier to test and debug and ensure its logic is fully functional and can be safely
    /// exposed to client so the client is only required to ensure the UI and visual representations are working.
    /// </summary>
    public class TestClientSDKSmoke
    {
        StandaloneServer _server;
        TestGameClient _client;

        [OneTimeSetUp]
        public void Setup()
        {
            _server = new StandaloneServer().Start();
            _client = new TestGameClient();
            _client.PrepareSDK();
        }

        [OneTimeTearDown]
        public void TearDown() => _server?.Dispose();

        [Test]
        [NonParallelizable]
        public async Task TestDisconnection()
        {
            _client.Modules.Account.SendAuthenticationPacket("abc", "def");
            _client.Network.Disconnect();
            var result = await _client.WaitFor<LoginResultPacket>();
            Assert.IsNull(result);
        }
        
        [Test]
        [NonParallelizable]
        public async Task SmokeTestFlow()
        {
            var mapPlacementUpdates = new List<IEntity>();
            _client.Modules.Components.OnComponentUpdate<MapPlacementComponent>((e, oldValue, newValue) =>
            {
                mapPlacementUpdates.Add(e);
            });

            // LOGIN
            _client.Modules.Account.SendAuthenticationPacket("abc", "def");
            var result = await _client.WaitFor<LoginResultPacket>();
            Assert.AreEqual(true, result.Success);

            // RECEIVE TILES
            var firstReceived = await _client.WaitFor<TileUpdatePacket>();
            Assert.NotNull(firstReceived);
            foreach (var tileUpdate in _client.ReceivedPackets.Where(p => p.GetType() == typeof(TileUpdatePacket)))
            {
                var tilePacket = (TileUpdatePacket)tileUpdate;
                var tile = _client.Game.World.Map.GetTile(tilePacket.Position.X, tilePacket.Position.Y);
                Assert.AreEqual(tile.X, tilePacket.Position.X);
                Assert.AreEqual(tile.Y, tilePacket.Position.Y);
                Assert.AreEqual(tile.SpecId, tilePacket.Data.TileId);
                Assert.AreEqual(new Location(tile.X >> GameWorld.CHUNK_SIZE_BITSHIFT, tile.Y >> GameWorld.CHUNK_SIZE_BITSHIFT), tile.Chunk.Position);
            }

            // RECEIVE ENTITIES
            var firstEntityReceived = await _client.WaitFor<EntityUpdatePacket>();
            Assert.NotNull(firstEntityReceived);
            foreach (var packet in _client.ReceivedPackets.Where(p => p.GetType() == typeof(EntityUpdatePacket)))
            {
                var entityUpdate = (EntityUpdatePacket)packet;
                var clientEntity = _client.Game.Entities[entityUpdate.EntityId];
                Assert.NotNull(clientEntity);
                Assert.AreEqual(clientEntity.EntityId, entityUpdate.EntityId);
                Assert.AreEqual(clientEntity.OwnerID, entityUpdate.OwnerId);
                // Entitty View was created
                Assert.NotNull(_client.Modules.Views.GetEntityView(clientEntity));
            }

            // CHECK TILE RESOURCES
            var map = _client.Game.World as GameWorld;
            foreach (var tile in map.AllTiles())
            {
                if (tile.Components.Has<TileResourceComponent>() && !tile.HasHarvestSpot)
                {
                    Assert.Fail($"Tile {tile} has invalid resource ?");
                }
            }

            // COMPONENTS SYNC TRIGGERED
            await _client.WaitFor<EntityUpdatePacket>(p => p.Type == EntityType.Party);
            Assert.AreEqual(2, mapPlacementUpdates.Count); // Party
            Assert.AreEqual(1, mapPlacementUpdates.Count(c => c is PartyEntity));
            mapPlacementUpdates.Clear();

            // CHECK IF CLIENT-SIDE EVENTS ARE FIRED
            Assert.Greater(_client.EventsInClientLogic.Count(e => e is TileVisibilityChangedForPlayerEvent), 0);

            // UPDATE ENTITY COMPONENTS
            foreach (var packet in _client.ReceivedPackets.Where(p => p is EntityUpdatePacket update && update.Type == EntityType.Party))
            {
                var partyUpdatePacket = (EntityUpdatePacket)packet;
                var clientParty = (PartyEntity)_client.Game.Entities[partyUpdatePacket.EntityId];

                var placement = clientParty.Get<MapPlacementComponent>();
                Assert.That(placement.Position.X > 0 && placement.Position.Y > 0);
            }

            // HARVEST
            _client.EventsInClientLogic.Clear();
            _client.ReceivedPackets.Clear();
            var party = _client.Modules.Player.LocalPlayer.GetParty(0);
            var resourceTile = party.Tile;
            foreach(var tile in party.Tile.GetAOE(2))
            {
                if(tile.HasHarvestSpot)
                {
                    _client.SDKLog.Debug($"Found harvest tile {tile}");
                    resourceTile = tile;
                    break;
                }
            }

            _client.Game.Log.Debug("--- Testing Movement Updates");
            var expectedMovementRange = resourceTile.Distance(party.Tile);
            Assert.IsTrue(_client.Modules.Actions.MoveParty(party, resourceTile, CourseIntent.Harvest));
            var update = await _client.WaitFor<EntityUpdatePacket>(p => p.SyncedComponents.Any(c => c.GetType()==typeof(HarvestingComponent)));
            var syncedHarvest = (HarvestingComponent)update.SyncedComponents.First(c => c.GetType() == typeof(HarvestingComponent));
            Assert.That(syncedHarvest.StartedAt != 0);

            // MOVEMENT EVENTS
            Assert.That(party.Tile == resourceTile);
            Assert.That(party.Get<MapPlacementComponent>().Position == resourceTile.Position);
            var moveEvents = _client.EventsInClientLogic.Where(e => e is EntityMoveInEvent).Cast<EntityMoveInEvent>();
            Assert.AreEqual(expectedMovementRange, moveEvents.Count());

            await Task.Delay(1000);

            // STOP HARVEST
            _client.ReceivedPackets.Clear();
            Assert.IsTrue(_client.Modules.Actions.StopParty(party));
            update = await _client.WaitFor<EntityUpdatePacket>(p => p.EntityId == party.EntityId);

            // Server should have remoevd the Harvesting Component
            Assert.IsFalse(update.SyncedComponents.Any(c => c.GetType() == typeof(HarvestingComponent)));
            Assert.IsTrue(update.RemovedComponentIds.Any(c => c == Serialization.GetTypeId(typeof(HarvestingComponent))));

            // Client needs to have also remoevd the harvesting component
            Assert.IsFalse(_client.Game.Entities[party.EntityId].Components.Has<HarvestingComponent>());

            // HARVEST CARGO
            var cargo = party.Get<CargoComponent>();
            Assert.That(cargo.Slot1.Amount > 0);
        }
    }
}
