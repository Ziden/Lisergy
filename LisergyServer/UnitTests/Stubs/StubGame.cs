using Game;
using Game.Engine;
using Game.Engine.DataTypes;
using Game.Engine.Network;
using Game.Engine.Scheduler;
using Game.Services;
using Game.Systems.Player;
using Game.Tile;
using Game.World;
using GameData;
using GameDataTest;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Game.Systems.Party;

namespace ServerTests
{
    public class TestGame : LisergyGame
    {
        protected GameId _testPlayerId = GameId.Generate();
        public GameServerNetwork TestNetwork { get; protected set; }
        public BattleService BattleService { get; protected set; }
        public WorldService WorldService { get; protected set; }
        public CourseService CourseService { get; protected set; }
        public List<BasePacket> SentServerPackets { get; protected set; } = new List<BasePacket>();
        public GameWorld TestWorld => World as GameWorld;
        public ServerChunkMap TestMap => TestWorld.Map as ServerChunkMap;

        private static GameSpec _testSpecs;

        protected virtual GameWorld CreateTestWorld()
        {
            Log.Debug("Setting Seed");
            WorldUtils.SetRandomSeed(666);
            Log.Debug("Creating World");
            var world = new GameWorld(this, 16, 16);
            SetupWorld(world);
            TestMap.SetFlag(0, 0, ChunkFlag.NEWBIE_CHUNK);
            return world;
        }

        public GameScheduler GameScheduler => this.Scheduler as GameScheduler;

        private static IGameLog GetTestLog()
        {
            var log = new GameLog("[Game]");
            log._Debug = m =>
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (lastLog != 0) m = $"[{now - lastLog}ms] " + m;
                lastLog = now;
                Console.WriteLine(m);
            };
            return log;
        }

        public TestGame(GameSpec specs = null, bool createWorld = true, bool createPlayer = true) : base(specs ?? GetTestSpecs(), GetTestLog())
        {
            GameId.DEBUG_MODE = 1;
            if (createWorld)
            {
                CreateTestWorld();
               
            }
            Log.Debug("Setting up serializer");
            Serialization.LoadSerializers();
            Log.Debug("Creating local services");
            BattleService = new BattleService(this);
            WorldService = new WorldService(this);
            CourseService = new CourseService(this);
            TestNetwork = Network as GameServerNetwork;
            GameScheduler.SetLogicalTime(DateTime.UtcNow);
            TestNetwork.OnOutgoingPacket += (player, packet) => ((TestServerPlayer)Players[player]).SendTestPacket(packet);
            if (createPlayer && createWorld)
                CreatePlayer();
            long originalByteCount = GC.GetTotalMemory(false) / 1000;
            Log.Debug($"Test Game Ready: Heap Allocation Total {originalByteCount}kb");
        }

        public void HandleClientEvent<T>(PlayerEntity sender, T ev) where T : BasePacket
        {
            var deserialized = Serialization.ToCastedPacket<BasePacket>(Serialization.FromBasePacket(ev));
            deserialized.Sender = sender;
            //ev.Sender = sender;
            TestNetwork.IncomingPackets.Call(deserialized);
            Entities.DeltaCompression.SendDeltaPackets(sender);
        }

        public TestServerPlayer CreatePlayer(in int x = 10, in int y = 10)
        {
            Log.Debug("Creating new Test Player");
            var player = new TestServerPlayer(this);
            player.OnReceivedPacket += ev => ReceivePacket(ev);
            _testPlayerId = player.EntityId;
            var tile = World.Map.GetTile(x, y);
            player.EntityLogic.Player.PlaceNewPlayer(World.Map.GetTile(x, y));
            Entities.DeltaCompression.SendDeltaPackets(player);
            //player.Parties[0].Components.Remove<ActionPointsComponent>();
            return player;
        }

        public void ReceivePacket(BasePacket ev)
        {
            SentServerPackets.Add(ev);
        }

        public TestServerPlayer GetTestPlayer()
        {
            PlayerEntity pl;
            this.World.Players.GetPlayer(_testPlayerId, out pl);
            return (TestServerPlayer)pl;
        }

        public static void BuildTestSpecs()
        {
            _testSpecs = TestSpecs.Generate();
            // Allow having initial building in tests
            _testSpecs.InitialBuildingSpecId = _testSpecs.Buildings[1].SpecId;
        }

        private static GameSpec GetTestSpecs()
        {
            if(_testSpecs == null)
            {
                BuildTestSpecs();
            }
            return _testSpecs;
        }

        public BuildingSpec RandomBuildingSpec()
        {
            return this.Specs.Buildings.Values.RandomElement();
        }


        public TileEntity RandomNotBuiltTile()
        {
            var tiles = TestWorld.AllTiles();
            foreach (var tile in tiles)
                if (tile.Building == null)
                    return tile;
            throw new System.Exception("No unbuilt tile");
        }

        private static long lastLog = 0;
    }

}
