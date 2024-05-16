using Game;
using Game.Engine.DataTypes;
using Game.Systems.Dungeon;
using Game.World;
using GameDataTest;
using NUnit.Framework;
using ServerTests;
using System.Linq;

namespace UnitTests
{
    public unsafe class TestTile
    {
        private TestGame Game;

        [SetUp]
        public void Setup()
        {
            Game = new TestGame();
        }

        [Test]
        public void TestTileAllocation()
        {

        }

        [Test]
        public void TestNeighboars()
        {
            var tile = Game.World.Map.GetTile(1, 1);

            Assert.AreEqual(Game.World.Map.GetTile(1, 1 + 1), tile.GetNeighbor(Direction.NORTH));
            Assert.AreEqual(Game.World.Map.GetTile(1, 1 - 1), tile.GetNeighbor(Direction.SOUTH));
            Assert.AreEqual(Game.World.Map.GetTile(1 - 1, 1), tile.GetNeighbor(Direction.WEST));
            Assert.AreEqual(Game.World.Map.GetTile(1 + 1, 1), tile.GetNeighbor(Direction.EAST));
        }

        [Test]
        public void TestDirections()
        {
            var tile = Game.World.Map.GetTile(1, 1);

            Assert.AreEqual(tile.GetDirection(Game.World.Map.GetTile(1, 1 + 1)), Direction.NORTH);
            Assert.AreEqual(tile.GetDirection(Game.World.Map.GetTile(1, 1 - 1)), Direction.SOUTH);
            Assert.AreEqual(tile.GetDirection(Game.World.Map.GetTile(1 + 1, 1)), Direction.EAST);
            Assert.AreEqual(tile.GetDirection(Game.World.Map.GetTile(1 - 1, 1)), Direction.WEST);
        }

        [Test]
        public void TestAddingStaticEntity()
        {
            var player = Game.GetTestPlayer();
            var building = player.Buildings.FirstOrDefault();
            var tile = building.Tile.GetNeighbor(Direction.NORTH);

            var dungeon = (DungeonEntity)Game.Entities.CreateEntity(GameId.ZERO, EntityType.Dungeon);
            Game.Systems.Map.GetLogic(dungeon).SetPosition(tile);

            Assert.AreEqual(dungeon.Tile, tile);
        }

        [Test]
        public void TestTilePassableFlag()
        {
            Assert.That(TestTiles.MOUNTAIN.MovementFactor == 0, "Mountain has to be totally impassable");
            Assert.That(TestTiles.GRASS.MovementFactor != 0, "Grass has to be passable");

            var tile = Game.World.Map.GetTile(0, 0);

            tile.SpecId = TestTiles.GRASS.ID;
            Assert.IsTrue(tile.Passable);
            Assert.AreEqual(tile.MovementFactor, TestTiles.GRASS.MovementFactor);

            tile.SpecId = TestTiles.MOUNTAIN.ID;
            Assert.IsFalse(tile.Passable);
            Assert.AreEqual(tile.MovementFactor, TestTiles.MOUNTAIN.MovementFactor);
        }

        [Test]
        public void TestTileId()
        {
            var tile = Game.World.Map.GetTile(5, 5);
            var tile2 = Game.World.Map.GetTile(6, 6);

            Assert.AreNotEqual(tile.EntityId, tile2.EntityId);
        }

        [Test]
        public void TestTileIdSame()
        {
            var tile = Game.World.Map.GetTile(5, 5);
            var tile2 = Game.World.Map.GetTile(5, 5);

            Assert.AreEqual(tile.EntityId, tile2.EntityId);
        }

        [Test]
        public void TestChunkId()
        {
            var tile = Game.World.Map.GetTile(9, 9);
            var chunk = tile.Chunk;

            Assert.That(chunk.X > 0);
            Assert.That(chunk.Y > 0);
            Assert.AreEqual(chunk.EntityId, new GameId(new Location(chunk.X, chunk.Y)));
        }

        [Test]
        public void TestTileUniqueIds()
        {
            var tile = Game.World.Map.GetTile(5, 5);
            var tile2 = Game.World.Map.GetTile(5, 5);

            Assert.AreEqual(tile.EntityId, tile2.EntityId);
        }
    }
}