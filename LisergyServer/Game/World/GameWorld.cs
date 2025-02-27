﻿using Game.Engine.DataTypes;
using Game.Tile;
using System;
using System.Collections.Generic;

namespace Game.World
{
    public interface IGameWorld : IDisposable
    {

        /// <summary>
        /// Gets the game this world belongs to
        /// </summary>
        public IGame Game { get; }

        /// <summary>
        /// Gets the players that reside in this world
        /// </summary>
        public IGamePlayers Players { get; }

        /// <summary>
        /// Gets the world map
        /// </summary>
        public IChunkMap Map { get; }

        /// <summary>
        /// Populates the given world using the chunk populators configured
        /// </summary>
        public void Populate();
    }

    public class GameWorld : IGameWorld
    {
        private GameId _id;
        public const int CHUNK_SIZE = 8;
        public static readonly int CHUNK_SIZE_BITSHIFT = CHUNK_SIZE.BitsRequired() - 1;
        public const int TILES_IN_CHUNK = CHUNK_SIZE * CHUNK_SIZE;
        public const int PLAYERS_CHUNKS = 2;

        public List<ChunkPopulator> ChunkPopulators { get; private set; } = new List<ChunkPopulator>();
        public virtual IGame Game { get; set; }
        protected ServerChunkMap _preallocatedMap { get; set; }
        public int Seed { get; set; }
        public ushort SizeX { get; private set; }
        public ushort SizeY { get; private set; }
        public IGamePlayers Players { get; protected set; }
        public IChunkMap Map { get; protected set; }

        public GameWorld(IGame game, in ushort sizeX, in ushort sizeY)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            Players = new WorldPlayers(int.MaxValue);
            Game = game;
            CreateMap();
        }

        public void FreeMemory()
        {
            foreach (var c in _preallocatedMap.AllChunks()) c.Dispose();
        }

        public virtual void CreateMap()
        {
            _id = GameId.Generate();
            _preallocatedMap = new ServerChunkMap(this, SizeX, SizeY);
            _preallocatedMap.CreateMap(SizeX, SizeY);
            Map = _preallocatedMap;
        }

        public void Populate()
        {
            if (ChunkPopulators.Count == 0) return;
            if (Seed == 0)
                Seed = WorldUtils.Random.Next(0, ushort.MaxValue);
            WorldUtils.SetRandomSeed(Seed);
            Game.Log.Debug($"Populating world seed {Seed} {SizeX}x{SizeY} for {Players.MaxPlayers} players");
            foreach (var chunk in _preallocatedMap.AllChunks())
                foreach (var populator in ChunkPopulators)
                    populator.Populate(this, _preallocatedMap, chunk);
        }

        public TileEntity GetUnusedStartingTile()
        {
            var freeChunk = _preallocatedMap.GetUnnocupiedNewbieChunk();
            if (freeChunk == null)
            {
                throw new Exception("No more room for newbie players in this world");
            }
            return freeChunk.FindTileWithId(0);
        }


        public virtual IEnumerable<TileEntity> AllTiles()
        {
            foreach (var chunk in _preallocatedMap.AllChunks())
                foreach (var tile in chunk.AllTiles())
                    yield return tile;
        }

        public void Dispose() => FreeMemory();
    }
}
