using Game.ECS;
using Game.Engine.DataTypes;
using Game.Engine.ECS;
using Game.Engine.Network;
using Game.Events;
using Game.Events.ServerEvents;
using Game.Systems.Battler;
using Game.Systems.Building;
using Game.Systems.Party;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Game.Systems.ActionPoints;

namespace Game.Systems.Player
{
    /// <summary>
    /// Represents a player in the world.
    /// A player is basically an owner of entities, and its an entity itself but its an entity that is not placed in the world.
    /// </summary>
    public class PlayerEntity : IEntity
    {
        public const byte MAX_PARTIES = 4;
        public PlayerData Data => Components.GetReference<PlayerData>();
        public VisibilityReferences VisibilityReferences => Components.GetReference<VisibilityReferences>();
        public PlayerProfile Profile { get; private set; }
        public ref readonly GameId EntityId => ref Profile.PlayerId;
        public ref readonly GameId OwnerID => ref Profile.PlayerId;

        public IGame Game { get; private set; }
        public PlayerEntity(PlayerProfile profile, IGame game)
        {
            Game = game;
            Components = new ComponentSet(this);
            Components.Add<PlayerComponent>();
            Components.Add<PlayerTurnsComponent>();
            Components.AddReference(new PlayerData());
            Components.AddReference(new VisibilityReferences());
            Profile = profile;
        }

        /// <summary>
        /// Gets a party of a given party slot for this player
        /// </summary>
        public PartyEntity GetParty(byte partyIndex) => Parties[partyIndex];

        /// <summary>
        /// Gets the main building (center) of this player
        public PlayerBuildingEntity GetCenter()
        {
            return Buildings.FirstOrDefault(b => b.SpecId == Game.Specs.InitialBuilding.SpecId);
        }

        /// <summary>
        /// Sends a message packet to display for this player client
        /// </summary>
        public void SendMessage(string msg, MessageType type = MessageType.RAW_TEXT)
        {
            Game.Network.SendToPlayer(new MessagePacket(MessageType.BAD_INPUT, msg), this);
        }

        public IReadOnlyCollection<Unit> OwnedUnits => Data.StoredUnits.Values;
        public IReadOnlyList<PartyEntity> Parties => OwnedEntities<PartyEntity>(EntityType.Party).ToList();
        public IReadOnlyList<PlayerBuildingEntity> Buildings => OwnedEntities<PlayerBuildingEntity>(EntityType.Building).ToList();

        public void AddOwnedEntity(IEntity entity)
        {
            Data.OwnedEntities[entity.EntityType].Add(entity.EntityId);
        }

        public IEnumerable<T> OwnedEntities<T>(EntityType type) where T : IEntity
        {
            foreach (var id in Data.OwnedEntities[type]) yield return (T)Game.Entities[id];
        }

        public IComponentSet Components { get; private set; }
        public IEntityLogic EntityLogic => Game.Logic.GetEntityLogic(this);
        public ref DeltaFlags DeltaFlags => throw new NotImplementedException();
        public EntityType EntityType => EntityType.Player;
        public override string ToString() => $"<Player id={EntityId}>";
        public ref T Get<T>() where T : unmanaged, IComponent => ref Components.Get<T>();
        public BasePacket GetUpdatePacket(PlayerEntity receiver, bool o) => throw new NotImplementedException();
        public void ProccessDeltas(PlayerEntity trigger) => throw new NotImplementedException();
        public void Save<T>(in T c) where T : unmanaged, IComponent => throw new NotImplementedException("Player for now cannot save components");
    }
}
