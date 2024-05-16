using Game.Engine.ECS;
using Game.Systems.Battler;
using Game.Systems.Map;
using Game.Systems.Movement;
using Game.Systems.Party;
using Game.Systems.Player;
using Game.Systems.Resources;

namespace Game
{
    /// <summary>
    /// A logic container for all the entity logic available.
    /// </summary>
    public interface IEntityLogic
    {  
        public MapLogic Map { get; }
        public BattleGroupLogic BattleGroup { get;}
        public PlayerLogic Player { get; }
        public CourseLogic Movement { get; }
        public HarvestingLogic Harvesting { get; }
        public CargoLogic Cargo { get; }
        public ActionPointsLogic ActionPoints { get; }
    }

    /// <summary>
    /// Wrapper around game logic to wrap for a specific entity
    /// </summary>
    public class EntityLogic : IEntityLogic
    {
        public MapLogic Map => _systems.Map.GetLogic(_entity);
        public BattleGroupLogic BattleGroup => _systems.BattleGroup.GetLogic(_entity);
        public PlayerLogic Player => _systems.Players.GetLogic(_entity);
        public CourseLogic Movement => _systems.EntityMovement.GetLogic(_entity);
        public HarvestingLogic Harvesting => _systems.Harvesting.GetLogic(_entity);
        public CargoLogic Cargo => _systems.Cargo.GetLogic(_entity);
        public ActionPointsLogic ActionPoints => _systems.ActionPoints.GetLogic(_entity);

        private ISystems _systems;
        private IEntity _entity;

        public EntityLogic(ISystems systems) { _systems = systems; }
        public IEntityLogic GetLogic(IEntity entity)
        {
            _entity = entity;
            return this;
        }
    }

    public interface IGameLogic
    {
        public IEntityLogic GetEntityLogic(IEntity e);
    }

    public class GameLogic : IGameLogic
    {
        private ISystems _systems;
        public EntityLogic _entityLogic;

        public GameLogic(ISystems systems)
        {
            _systems = systems;
            _entityLogic = new EntityLogic(systems);
        }

        /// <summary>
        /// Gets a reusable logic object for the given entity
        /// </summary>
        public IEntityLogic GetEntityLogic(IEntity e) => _entityLogic.GetLogic(e);
    }
}
