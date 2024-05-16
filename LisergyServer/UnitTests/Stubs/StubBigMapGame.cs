using Game;
using Game.Engine.DataTypes;
using Game.Network;
using Game.Services;
using Game.Systems.Player;
using Game.Tile;
using Game.World;
using GameData;
using GameDataTest;
using GameDataTest.TestWorldGenerator;
using System;
using System.Collections.Generic;

namespace ServerTests
{
    public class TestLargeGame : TestGame
    {
        protected override GameWorld CreateTestWorld()
        {
            GameId.DEBUG_MODE = 1;
            WorldUtils.SetRandomSeed(666);
            var world = new TestWorld(this);
            SetupWorld(world);
            Entities.DeltaCompression.ClearDeltas();
            return world;
        }
    }

}
