using Game.ECS;
using Game.Systems.Battler;
using Game.Systems.Building;
using Game.Systems.Dungeon;
using Game.Systems.FogOfWar;
using Game.Systems.Movement;
using Game.Systems.Party;
using Game.Systems.Tile;
using System.Collections.Generic;
using System;
using Game.Systems.Map;
using Game.Systems.Player;
using Game.Systems.Resources;
using System.Linq;
using System.Reflection;
using Game.Engine.Events;
using Game.Engine.ECS;
using Game.Engine.DataTypes;

namespace Game
{
    public interface ISystems {
        BuildingSystem Building { get; }
        BattleGroupSystem BattleGroup { get; }
        PlayerBuildingSystem PlayerBuilding { get; }
        DungeonSystem Dungeons { get; }
        EntityVisionSystem EntityVision { get; }
        CourseSystem EntityMovement { get; }
        TileVisibilitySystem TileVisibility { get; }
        MapSystem Map { get; }
        PartySystem Party { get; }
        TileSystem Tile { get; }
        PlayerSystem Players { get; }
        HarvestingSystem Harvesting { get; }
        ResourceSystem Resources { get; }
        CargoSystem Cargo { get; }
        ActionsPointSystem ActionPoints { get; }
        void CallEvent(IEntity entity, IBaseEvent ev);
    }

    public class GameSystems : ISystems
    {
        private readonly DefaultValueDictionary<Type, List<IGameSystem>> _systems = new DefaultValueDictionary<Type, List<IGameSystem>>();

        private LisergyGame _game;

        public GameSystems(LisergyGame game)
        {
            _game = game;
            AddSystem(Building = new BuildingSystem(game));
            AddSystem(BattleGroup = new BattleGroupSystem(game));
            AddSystem(PlayerBuilding = new PlayerBuildingSystem(game));
            AddSystem(Dungeons = new DungeonSystem(game));
            AddSystem(EntityVision = new EntityVisionSystem(game));
            AddSystem(EntityMovement = new CourseSystem(game));
            AddSystem(TileVisibility = new TileVisibilitySystem(game));
            AddSystem(Party = new PartySystem(game));
            AddSystem(Tile = new TileSystem(game));
            AddSystem(Map = new MapSystem(game));
            AddSystem(Players = new PlayerSystem(game));
            AddSystem(Harvesting = new HarvestingSystem(game));
            AddSystem(Resources = new ResourceSystem(game));
            AddSystem(Cargo = new CargoSystem(game));
            AddSystem(ActionPoints = new ActionsPointSystem(game));
        }

        public MapSystem Map { get; private set; }
        public BuildingSystem Building { get; private set; }
        public BattleGroupSystem BattleGroup { get; private set; }
        public PlayerBuildingSystem PlayerBuilding { get; private set; }
        public DungeonSystem Dungeons { get; private set; }
        public EntityVisionSystem EntityVision { get; private set; }
        public CourseSystem EntityMovement { get; private set; }
        public TileVisibilitySystem TileVisibility { get; private set; }
        public PartySystem Party { get; private set; }
        public TileSystem Tile { get; private set; }
        public PlayerSystem Players { get; private set; }
        public HarvestingSystem Harvesting { get; private set; }
        public ResourceSystem Resources { get; private set; }
        public CargoSystem Cargo { get; private set; }
        public ActionsPointSystem ActionPoints { get; private set; }

        private void AddSystem<ComponentType>(GameSystem<ComponentType> system) where ComponentType : unmanaged, IComponent 
        {
            _systems[typeof(ComponentType)].Add(system);
            if(_game.IsClientGame)
            {
                var sync = system.GetType().GetCustomAttribute(typeof(SyncedSystem)) as SyncedSystem;
                if (sync == null) return;
            }
            _game.Log.Debug($"Registered System {system.GetType().Name}");
            system.RegisterListeners();
        }

        public void CallEvent(IEntity entity, IBaseEvent ev)
        {
            _game.Events.Call(ev);
            foreach (var componentType in entity.Components.All().ToArray())
            {
                CallSystemEvent(componentType, entity, ev);
            }
        }

        private void CallSystemEvent<EventType>(Type componentType, IEntity entity, EventType ev) where EventType : IBaseEvent
        {
            if (_systems.TryGetValue(componentType, out var systems))
            {
                foreach(var system in systems) system.CallEvent(entity, ev);
            }
            
        }
    }
}
