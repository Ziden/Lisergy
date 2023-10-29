﻿using Game.Battle.Data;
using Game.DataTypes;
using Game.ECS;
using Game.Network.ServerPackets;
using Game.Systems.Battler;
using Game.Systems.Building;
using Game.Systems.Party;
using Game.Systems.Tile;
using Game.Tile;
using Game.World;
using GameData;
using GameData.Specs;

namespace Game.Systems.Player
{
    public class PlayerLogic : BaseEntityLogic<PlayerComponent>
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

        public IEntity CreateNewParty(byte index)
        {
            var entity = (PartyEntity)Game.Entities.CreateEntity(Entity.EntityId, EntityType.Party);
            var party = entity.Get<PartyComponent>();
            party.PartyIndex = index;
            entity.Save(party);
            return entity;
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
