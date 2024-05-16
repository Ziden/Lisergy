using System;
using Game.Engine.DataTypes;
using Game.Engine.ECS;
using Game.Engine.Events;
using Game.Systems.Harvesting;
using Game.Systems.Map;
using Game.Tile;
using Game.World;
using GameData;

namespace Game.Systems.Resources
{
    /// <summary>
    /// Small predictive snapshot of the current harvesting state
    /// </summary>
    public struct HarvestingTaskState
    {
        /// <summary>
        /// Harvesting task prediction data
        /// </summary>
        public TimeBlockSnapshot TimeSnapshot;

        /// <summary>
        /// What are the resources and amount being harvested
        /// </summary>
        public TileResourceComponent Resources;

        /// <summary>
        /// How many resource was already harvested
        /// </summary>
        public ushort AmountHarvested;

        /// <summary>
        /// How many units of the resource still fits the cargo ?
        /// </summary>
        public ushort CargoAvailableForUnits;
    }

    /// <summary>
    /// Logic for any entity that has a cargo component meaning he can harvest resources
    /// </summary>
    public unsafe class HarvestingLogic : BaseEntityLogic<HarvesterComponent>
    {
        /// <summary>
        /// Checks if the current entity can harvest the given tile
        /// </summary>
        public bool CanHarvest(TileEntity tile)
        {
            var harvest = GetAvailableResourcesToHarvest(tile);
            if (Entity.Components.Has<HarvestingComponent>()) return false;
            if (harvest.Amount <= 0) return false;
            var cargo = Entity.Get<CargoComponent>();
            if (cargo.GetRoomFor(harvest.ResourceId) == -1) return false;
            var resourceSpec = Game.Specs.Resources[harvest.ResourceId];
            if (cargo.RemainingWeight < resourceSpec.WeightPerUnit) return false;
            var pos = Entity.Get<MapPlacementComponent>();
            return pos.Position.Distance(tile.Position) <= 1;
        }

        /// <summary>
        /// Gets the available resources to be harvested on a given tile
        /// </summary>
        public ResourceStackData GetAvailableResourcesToHarvest(TileEntity tile)
        {
            if (!tile.Components.TryGet<TileResourceComponent>(out var harvest))
            {
                return default;
            }
            else return harvest.Resource;
        }

        /// <summary>
        /// Returns if given entity is currently harvesting
        /// </summary>
        public bool IsHarvesting() => Entity.Components.Has<HarvestingComponent>();

        /// <summary>
        /// Starts harvesting the given tile.
        /// </summary>
        public bool StartHarvesting(TileEntity tile)
        {
            if (!CanHarvest(tile)) return false;
            Game.Log.Debug($"{Entity} starting harvesting on {tile}");
            var tileResources = tile.Components.GetPointer<TileResourceComponent>();
            tileResources->BeingHarvested = true;
            Entity.Components.Add<HarvestingComponent>();
            var harvesting = Entity.Components.GetPointer<HarvestingComponent>();
            harvesting->Tile = tile.Position;
            harvesting->StartedAt = Game.GameTime.ToBinary();
            var ev = EventPool<HarvestingStartedEvent>.Get();
            ev.Tile = tile;
            ev.Harvester = Entity;
            ev.Resource = tileResources->Resource.ResourceId;
            Entity.Components.CallEvent(ev);
            EventPool<HarvestingStartedEvent>.Return(ev);
            return true;
        }

        /// <summary>
        /// Given the current entity max cargo and resources on the given tile
        /// Gets the amount of resource that could be harvested from a given tile
        /// </summary>
        public (TimeBlock time, ResourceStackData resource)? GetPossibleHarvest(TileEntity tile)
        {
            var tileSpec = Game.Specs.Tiles[tile.SpecId];
            if (!tileSpec.ResourceSpotSpecId.HasValue)
            {
                return default;
            }
            var harvestSpec = Game.Specs.HarvestPoints[tileSpec.ResourceSpotSpecId.Value];
            var tileResources = tile.Components.Get<TileResourceComponent>();
            var toHarvest = tileResources.Resource;
            Entity.EntityLogic.Cargo.TrimResourcesToMaxCargo(ref toHarvest);
            var timeToHarvest = toHarvest.Amount * harvestSpec.HarvestTimePerUnit;
            var block = new TimeBlock()
            {
                StartTime = Game.GameTime,
                EndTime = Game.GameTime + timeToHarvest
            };
            return (block, toHarvest);
        }

        /// <summary>
        /// Calculates what's the current state of the harvesting task.
        /// </summary>
        public HarvestingTaskState CalculateCurrentState()
        {
            var harvesting = Entity.Components.Get<HarvestingComponent>();
            var tile = Game.World.Map.GetTile(harvesting.Tile.X, harvesting.Tile.Y);
            var startTime = DateTime.FromBinary(harvesting.StartedAt);
            var tileSpec = Game.Specs.Tiles[tile.SpecId];
            if (!tileSpec.ResourceSpotSpecId.HasValue)
            {
                return default;
            }
            var harvestSpec = Game.Specs.HarvestPoints[tileSpec.ResourceSpotSpecId.Value];
            var tileResources = tile.Components.Get<TileResourceComponent>();
            var totalTaskTime = (harvestSpec.HarvestTimePerUnit * tileResources.Resource.Amount);
            var timeBlock = startTime.GetTimeBlock(totalTaskTime);
            var timeSnapshot = timeBlock.GetCurrentSnapshot(Game.GameTime);
            var possible = GetPossibleHarvest(tile);
            var harvested = possible.Value.resource;
            harvested.Amount = (ushort)Math.Round(harvested.Amount * timeSnapshot.Percentagage);
            return new HarvestingTaskState()
            {
                AmountHarvested = harvested.Amount,
                Resources = tileResources,
                TimeSnapshot = timeSnapshot
            };
        }

        /// <summary>
        /// Stops harvesting the resource the entity is harvesting.
        /// The amount of resources collected will be calculated at this time
        /// </summary>
        public ResourceStackData StopHarvesting()
        {
            var harvesting = Entity.Components.Get<HarvestingComponent>();
            var tile = Game.World.Map.GetTile(harvesting.Tile.X, harvesting.Tile.Y);
            var harvestState = CalculateCurrentState();
            var spec = Game.Specs.Resources[harvestState.Resources.Resource.ResourceId];

            Entity.Components.Remove<HarvestingComponent>();
            var tileResources = tile.Components.GetPointer<TileResourceComponent>();
            tileResources->BeingHarvested = false;
            tileResources->Resource.Amount = (ushort)(tileResources->Resource.Amount - harvestState.AmountHarvested);
            var finalStack = new ResourceStackData(harvestState.Resources.Resource.ResourceId, harvestState.AmountHarvested);
            var ev = EventPool<HarvestingEndedEvent>.Get();
            ev.Tile = tile;
            ev.Harvester = Entity;
            ev.Resource = finalStack;
            Entity.Components.CallEvent(ev);
            EventPool<HarvestingEndedEvent>.Return(ev);
            Game.Log.Debug($"{Entity} finished harvesting on {tile} and got {harvestState.AmountHarvested}x{spec.Name}");
            return finalStack;
        }
    }
}