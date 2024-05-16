using Game.Engine.DataTypes;
using Game.Engine.ECS;
using Game.Systems.Battle.Data;
using Game.Systems.Battler;
using Game.Systems.Building;
using Game.Systems.Party;
using Game.Tile;
using Game.World;
using GameData;
using GameData.Specs;
using System.Collections.Generic;
using Game.Systems.ActionPoints;

namespace Game.Systems.Player
{
    /// <summary>
    /// Interacts with player specific data
    /// </summary>
    public unsafe class PlayerLogic : BaseEntityLogic<PlayerComponent>
    {
        private PlayerData Data => Entity.Components.GetReference<PlayerData>();
 
        /// <summary>
        /// Recruits a new unit for the player
        /// </summary>
        public Unit RecruitUnit(UnitSpecId unitSpecId)
        {
            var unit = new Unit(Game.Specs.Units[unitSpecId]);
            Data.StoredUnits.Add(unit.Id, unit);
            Game.Log.Debug($"{Entity} recruited {unit}");
            return unit;
        }

        /// <summary>
        /// Creates a new party for the player on the given party index
        /// </summary>
        public IEntity CreateNewParty(byte index)
        {
            var entity = (PartyEntity)Game.Entities.CreateEntity(Entity.EntityId, EntityType.Party);
            var party = entity.Get<PartyComponent>();
            party.PartyIndex = index;
            entity.Save(party);
            return entity;
        }

        /// <summary>
        /// Gets what the owner of the given entity can build at this moment
        /// </summary>
        public List<BuildingConstructionSpec> GetBuildingOptions()
        {
            var list = new List<BuildingConstructionSpec>();
            foreach (var kp in Game.Specs.BuildingConstructions)
            {
                list.Add(kp.Value);
            }
            return list;
        }

        /// <summary>
        /// Adds a new player to the given tile with the initial things a new player should have
        /// </summary>
        public void PlaceNewPlayer(TileEntity t)
        {
            var p = Entity as PlayerEntity;
            Game.Log.Debug($"Placing new player {p} on tile {t}");
            Game.Players.Add(p);
            if (Game.Specs.InitialBuildingSpecId.HasValue)
            {
                Build(Game.Specs.InitialBuildingSpecId.Value, t);
            }
            var initialUnit = Game.Specs.InitialUnit.SpecId;
            var unit = RecruitUnit(initialUnit);
            var party = CreateNewParty(0);
            PlaceUnitInParty(unit.Id, (PartyEntity)party);
            var partyTile = t.GetNeighbor(Direction.EAST);
            if(!partyTile.Passable) partyTile = t.GetNeighbor(Direction.WEST);
            if (!partyTile.Passable) partyTile = t.GetNeighbor(Direction.SOUTH);
            if (!partyTile.Passable) partyTile = t.GetNeighbor(Direction.NORTH);
            Game.Systems.Map.GetLogic(party).SetPosition(partyTile);
            Game.Log.Debug($"Placed new player {Entity} in {t}");
            return;
        }

        public void TakeTurn()
        {
            var p = Entity as PlayerEntity;
            foreach (var party in p.Parties)
            {
                party.EntityLogic.ActionPoints.SetActionPoints(1);
            }
            p.Components.GetPointer<PlayerTurnsComponent>()->Turns++;
        }

        /// <summary>
        /// Record a battle header of a battle that happened for this player
        /// </summary>
        public void RecordBattleHeader(BattleHeader header)
        {
            Data.BattleHeaders.Add(header);
        }

        /// <summary>
        /// Moves a unit to a given party
        /// </summary>
        public void PlaceUnitInParty(GameId unitId, PartyEntity newParty)
        {
            if(!Data.StoredUnits.TryGetValue(unitId, out var storedUnit))
            {
                Game.Log.Error($"Tried to add unity {unitId} to party but unit was not free");
                return;
            }
            Data.StoredUnits.Remove(unitId);
            Game.Logic.GetEntityLogic(newParty).BattleGroup.AddUnit(storedUnit);
            Game.Log.Debug($"{Entity} moved unit {storedUnit} to party {newParty}");
        }

        /// <summary>
        /// Builds a new building on the given tile
        /// </summary>
        public PlayerBuildingEntity Build(BuildingSpecId buildingSpecId, TileEntity t)
        {
            var building = (PlayerBuildingEntity)Game.Entities.CreateEntity(Entity.OwnerID, EntityType.Building);
            building.BuildFromSpec(Game.Specs.Buildings[buildingSpecId]);
            building.EntityLogic.Map.SetPosition(t);
            Game.Log.Debug($"Player {Entity} built {building}");
            return building;
        }
    } 
}
