using Game;
using Game.ECS;
using Game.Engine.DataTypes;
using Game.Systems.Building;
using Game.Systems.Dungeon;
using Game.Systems.Map;
using Game.Systems.Tile;
using Game.Tile;
using NUnit.Framework;
using ServerTests;

namespace UnitTests
{
    public unsafe class TestTileComponents
    {
        private IGame _game;
        private TileEntity tile1;
        private TileEntity tile2;

        [SetUp]
        public void Setup()
        {
            _game = new TestGame();
            tile1 = _game.World.Map.GetTile(0, 0);
            tile2 = _game.World.Map.GetTile(1, 1);
        }

        [Test]
        public void TestTileCallbacks()
        {
            tile1.Components.AddReference(new TileHabitantsReferenceComponent());
            tile2.Components.AddReference(new TileHabitantsReferenceComponent());

            var dg = (DungeonEntity)_game.Entities.CreateEntity(GameId.ZERO, EntityType.Dungeon);

            tile1.Components.CallEvent(new BuildingPlacedEvent(dg, tile1));

            Assert.IsTrue(tile1.Components.GetReference<TileHabitantsReferenceComponent>().Building == dg);
            Assert.IsTrue(tile2.Components.GetReference<TileHabitantsReferenceComponent>().Building == null);
        }

        public class TestView : IComponent
        {
            public static EntityMoveOutEvent called = default;

            public static void Callback(TestView view, EntityMoveOutEvent ev)
            {
                TestView.called = ev;
            }
        }
    }
}