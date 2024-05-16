using BaseServer.Persistence;
using Game;
using Game.Engine.ECS;
using Game.World;
using NUnit.Framework;
using ServerTests;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests
{
    public class TestPersistence
    {
        [Test]
        public async Task TestSavingWorldToFile()
        {
            var game1 = new TestGame();
            game1.CreatePlayer();

            var persistence = new FlatFileWorldPersistence(game1.Log);

            await persistence.Save(game1, "TestWorld");

            var game2 = await persistence.Load(game1.Specs, "TestWorld");

            CompareGames(game1, game2);
        }

        [Test]
        public void TestSavingWorld()
        {
            var game1 = new TestGame();
            game1.CreatePlayer();
            var world1 = game1.World as GameWorld;
            var mapSize = (ushort)game1.World.Map.TilemapDimensions.x;
            
            // Messing the tilemap to ensure its all correct
            var random = new Random();
            foreach (var tile in world1.AllTiles()) tile.SpecId = (byte)(random.NextSingle() * 3);

            FlatFileWorldPersistence persistence = new FlatFileWorldPersistence(null);

            var worldBytes = persistence.SerializeMap(game1);
            var playerBytes = persistence.SerializePlayers(game1);
            var entityBytes = persistence.SerializeEntities(game1);

            var game2 = new LisergyGame(game1.Specs, game1.Log);
            var world2 = new GameWorld(game2, mapSize, mapSize);
            game2.SetupWorld(world2);

            persistence.DeserializeMap(game2, worldBytes);
            persistence.DeserializePlayers(game2, playerBytes);
            persistence.DeserializeEntities(game2, entityBytes);

            CompareGames(game1, game2);
        }

        private void CompareGames(LisergyGame game1, LisergyGame game2)
        {
            var mapSize = (ushort)game1.World.Map.TilemapDimensions.x;
            var world1 = game1.World;
            var world2 = game2.World;
            // CHECKING TILES
            for (var x = 0; x < mapSize; x++)
            {
                for (var y = 0; y < mapSize; y++)
                {
                    Assert.AreEqual(world1.Map.GetTile(x, y).SpecId, world2.Map.GetTile(x, y).SpecId);
                }
            }

            // CHECKING PLAYERS
            Assert.AreEqual(game1.World.Players.AllPlayers().Count, game2.World.Players.AllPlayers().Count);
            foreach (var p1 in game1.World.Players.AllPlayers())
            {
                var p2 = game2.World.Players[p1.EntityId];

                Assert.IsTrue(p1.Data.OwnedEntities[EntityType.Party].SequenceEqual(p2.Data.OwnedEntities[EntityType.Party]));
                Assert.IsTrue(p1.Data.OwnedEntities[EntityType.Building].SequenceEqual(p2.Data.OwnedEntities[EntityType.Building]));
                Assert.IsTrue(p1.Data.StoredUnits.Values.SequenceEqual(p2.Data.StoredUnits.Values));
                Assert.IsTrue(p1.VisibilityReferences.OnceExplored.SequenceEqual(p2.VisibilityReferences.OnceExplored));
                Assert.IsTrue(p1.VisibilityReferences.VisibleTiles.SequenceEqual(p2.VisibilityReferences.VisibleTiles));
                foreach (var visible in p1.VisibilityReferences.VisibleTiles)
                {
                    var tile = game2.World.Map.GetTile(visible.X, visible.Y);
                    Assert.IsTrue(tile.PlayersViewing.Contains(p2));
                    Assert.IsTrue(tile.EntitiesViewing.Any(e => e.OwnerID == p2.EntityId));
                }
            }

            // CHECKING ENTITIES
            Assert.AreEqual(((GameEntities)game1.Entities).AllEntities.Count, ((GameEntities)game2.Entities).AllEntities.Count);
            foreach (var e1 in ((GameEntities)game1.Entities).AllEntities)
            {
                var e2 = game2.Entities[e1.EntityId];

                var e1c = (ComponentSet)e1.Components;
                var e2c = (ComponentSet)e2.Components;

                Assert.IsTrue(e1c._pointerComponents.ToArray().SequenceEqual(e2c._pointerComponents.ToArray()));

                if (e1 is BaseEntity b1 && e2 is BaseEntity b2)
                {
                    Assert.AreEqual(b1.Tile.Position, b2.Tile.Position);
                }
            }
        }
    }
}
