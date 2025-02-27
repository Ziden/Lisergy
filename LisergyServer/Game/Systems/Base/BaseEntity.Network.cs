﻿using Game.Events.ServerEvents;
using System.Collections.Generic;
using Game.Systems.Movement;
using Game.Systems.Player;
using Game.Systems.MapPosition;
using System.Linq;
using System;
using System.Runtime.CompilerServices;
using Game.ECS;
using Game.Engine.Network;
using Game.Engine;

namespace Game
{
    /// <summary>
    /// Networking code for entities.
    /// Entity modifications are tracked by delta flags.
    /// Then after a "server tick" we dispatch all packets needed according to entity deltas
    /// </summary>
    public partial class BaseEntity
    {
        /// <summary>
        /// Cache to re-use the same hashset for all viewers lookups
        /// </summary>
        private static HashSet<PlayerEntity> _stoppedSeeingEntity = new HashSet<PlayerEntity>();
        private DeltaFlags _flags;
        public ref DeltaFlags DeltaFlags { get => ref _flags; }

       
        public void ProccessDeltas(PlayerEntity trigger)
        {
            if (DeltaFlags.HasFlag(DeltaFlag.CREATED)) OnExistenceChanged();
            else if (DeltaFlags.HasFlag(DeltaFlag.COMPONENTS)) SendUpdateToNewViewers();
            else if (DeltaFlags.HasFlag(DeltaFlag.SELF_REVEALED)) OnRevealed(trigger);
        }

        /// <summary>
        /// Sends the whole enity to whoever can see it whenever entity is created
        /// </summary>
       
        private void OnExistenceChanged()
        {
            var c = Components.GetReference<MapReferenceComponent>();
            if (c.Tile == null) return;
            Game.Log.Debug($"Entity {this} Had DeltaFlag 'EXISTENCE' - Sending Packets");
            foreach (var playerViewing in c.Tile.PlayersViewing)
                Game.Network.SendToPlayer(GetUpdatePacket(playerViewing, onlyDeltas: false), playerViewing);
        }

        /// <summary>
        /// When entity is revealed send a full update only to the player that triggered the delta
        /// </summary>
       
        private void OnRevealed(PlayerEntity trigger)
        {
            Game.Log.Debug($"Entity {this} Had DeltaFlag 'REVEALED' - Sending Packets");
            Game.Network.SendToPlayer(GetUpdatePacket(trigger, onlyDeltas: false), trigger);
        }

        /// <summary>
        /// We send component updates to all viwers
        /// But if the map position has updated then we also need to send the update
        /// for the old viwers so they can see the entity moving our of their view
        /// </summary>
        private void SendUpdateToNewViewers()
        {
            Game.Log.Debug($"Entity {this} Had DeltaFlag 'COMPONENTS' - Sending Packets");
            var c = Components.GetReference<MapReferenceComponent>();       
            var newTile = c.Tile;
            var previousTile = c.PreviousTile;

            if (!Components.Has<CourseComponent>()) return;

            if(newTile != previousTile)
            {
                c.PreviousTile = null;
                _stoppedSeeingEntity.Clear();
                if(previousTile != null) _stoppedSeeingEntity.UnionWith(previousTile.PlayersViewing);
                if(newTile != null) _stoppedSeeingEntity.ExceptWith(newTile.PlayersViewing);
                foreach (var stoppedSeeingEntity in _stoppedSeeingEntity)
                    Game.Network.SendToPlayer(GetUpdatePacket(stoppedSeeingEntity), stoppedSeeingEntity);
            }
            
            foreach(var seeingEntity in newTile.PlayersViewing)
                Game.Network.SendToPlayer(GetUpdatePacket(seeingEntity), seeingEntity);
        }

        /// <summary>
        /// Gets the base update packet of the given entity.
        /// Will add only updated components if onlyDeltas is toggled
        /// </summary>
        public BasePacket GetUpdatePacket(PlayerEntity receiver, bool onlyDeltas = true)
        {
            var packet = PacketPool.Get<EntityUpdatePacket>();
            packet.EntityId = EntityId;
            packet.OwnerId = OwnerID;
            packet.Type = EntityType;
            var deltas = Components.GetComponentDeltas(receiver, onlyDeltas);
            packet.SyncedComponents = deltas.updated.ToArray();
            packet.RemovedComponentIds = deltas.removed.Select(c => Serialization.GetTypeId(c)).ToArray();
            Game.Log.Debug($"Sync components [{string.Join(",", packet.SyncedComponents.Select(c => c.GetType().Name))}] for entity {this}");
            if (packet.SyncedComponents.Length == 0) throw new Exception("Trying to sync entity without modifying any component");
            return packet;
        }
    }
}
