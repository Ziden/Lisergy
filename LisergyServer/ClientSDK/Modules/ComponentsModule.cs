﻿using ClientSDK.SDKEvents;
using Game.ECS;
using Game.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Events.ServerEvents;
using Game.Engine.ECS;
using Game.Engine;

namespace ClientSDK.Modules
{
    /// <summary>
    /// Enables the game to more specifically control how to update components.
    /// Components can be synced automatically (just copy data) or have manual syncs to handle things manually.
    /// This module is able to implement callbacks from when a given component is synced so the client can react to it.
    /// </summary>
    public interface IComponentsModule : IClientModule
    {
        /// <summary>
        /// Updates the components of the given entity
        /// </summary>
        void ProccessUpdate(IEntity entity, IComponent[] updated, uint [] removed);

        /// <summary>
        /// Registers a component sync. 
        /// Whenever the given entity type has the given component type updated, instead of the values simply being copied
        /// the sync code will be called.
        /// The callback has the Entity, OLD VALUE and NEW VALUE parameters.
        /// </summary>
        void OnComponentUpdate<ComponentType>(Action<IEntity, ComponentType, ComponentType> OnSync) where ComponentType : IComponent;

        /// <summary>
        /// Registers a component removed callback. 
        /// Whenever the given entity type has the given component type removed, the callback will be called after the operation has been done..
        /// The callback has the Entity, OLD VALUE and NEW VALUE parameters.
        /// </summary>
        void OnComponentRemoved<ComponentType>(Action<IEntity, ComponentType> OnRemoved) where ComponentType : IComponent;

        /// <summary>
        /// Removes all event callbacks from the given object
        /// </summary>
        void RemoveListener(object listener);
    }

    public class ComponentsModule : IComponentsModule
    {
        private Dictionary<Type, List<Delegate>> _componentRemovals = new Dictionary<Type, List<Delegate>>();
        private Dictionary<Type, List<Delegate>> _componentSyncs = new Dictionary<Type, List<Delegate>>();
        private Dictionary<Type, List<Type>> _listeners = new Dictionary<Type, List<Type>>();

        private List<(IComponent, IComponent)> _toSync = new List<(IComponent, IComponent)>();
        private IGameClient _client;

        public ComponentsModule(IGameClient client) {
            _client = client;
        }

        public void Register() {}

        public void RemoveListener(object listener)
        {
            if (!_listeners.TryGetValue(listener.GetType(), out var listeners)) return;

            foreach (var t in listeners)
            {
                if (_componentRemovals.TryGetValue(t, out var removals))
                {
                    foreach (var r in new List<Delegate>(removals))
                    {
                        if (r.Target == listener)
                        {
                            removals.Remove(r);
                        }
                    }
                }
                if (_componentSyncs.TryGetValue(t, out var syncs))
                {
                    foreach (var r in new List<Delegate>(syncs))
                    {
                        if (r.Target == listener)
                        {
                            syncs.Remove(r);
                        }
                    }
                }
            }
        }

        public void OnComponentRemoved<ComponentType>(Action<IEntity, ComponentType> OnSync) where ComponentType : IComponent
        {
           
            var t = typeof(ComponentType);
            if (!_componentRemovals.TryGetValue(t, out var syncList))
            {
                syncList = new List<Delegate>();
                _componentRemovals[t] = syncList;
            }
            if (!_listeners.TryGetValue(OnSync.Target.GetType(), out var listeners))
            {
                listeners = new List<Type>();
                _listeners[OnSync.Target.GetType()] = listeners;
            }
            listeners.Add(t);
            syncList.Add(OnSync);
        }

        public void OnComponentUpdate<ComponentType>(Action<IEntity, ComponentType, ComponentType> OnSync) where ComponentType : IComponent
        {
            var t = typeof(ComponentType);
            if (!_componentSyncs.TryGetValue(t, out var syncList))
            {
                syncList = new List<Delegate>();
                _componentSyncs[t] = syncList;
            }
            if (!_listeners.TryGetValue(OnSync.Target.GetType(), out var listeners)) {
                listeners = new List<Type>();
                _listeners[OnSync.Target.GetType()] = listeners;
            }
            listeners.Add(t);
            syncList.Add(OnSync);
        }

        /// <summary>
        /// Updates the components of the given entity.
        /// Will copy all new values to old values
        /// Any registered component sync callbacks will be called after all updates are done
        /// </summary>
        public void ProccessUpdate(IEntity currentEntity, IComponent [] updated, uint [] removed)
        {
            _toSync.Clear();

            // Updating existing components
            foreach (var newComponent in updated)
            {
                if (_componentSyncs.ContainsKey(newComponent.GetType()))
                {
                    _toSync.Add((currentEntity.Components.GetByType(newComponent.GetType()), newComponent));
                }
                currentEntity.Components.Save(newComponent);
            }

            // Removing removed components
            foreach (var removedId in removed)
            {
                var componentType = Serialization.GetType(removedId);
                var comp = currentEntity.Components.GetByType(componentType);
                currentEntity.Components.Remove(componentType);
                if (_componentRemovals.TryGetValue(componentType, out var removals))
                {
                    foreach (var deleg in removals)
                    {
                        deleg.DynamicInvoke(currentEntity, comp);
                    }
                }
            }

            // Triggering all sync callbacks
            foreach(var toSync in _toSync)
            {
                foreach(var deleg in _componentSyncs[toSync.Item2.GetType()])
                {
                    deleg.DynamicInvoke(currentEntity, toSync.Item1, toSync.Item2);
                }
            }
        }
    }
}
