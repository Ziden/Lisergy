using ClientSDK;
using ClientSDK.Data;
using ClientSDK.SDKEvents;
using Game.Engine;
using Game.Engine.DataTypes;
using Game.Engine.Events;
using Game.Engine.Events.Bus;
using Game.Engine.Network;
using Game.Events.ServerEvents;
using Game.Systems.Building;
using Game.Systems.Dungeon;
using Game.Systems.Party;
using Game.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerTests.Integration.Stubs
{
    internal class TestGameClient : GameClient , IDisposable, IEventListener
    {
        public new ClientNetwork Network { get; private set; }
        public List<BasePacket> ReceivedPackets { get; private set; } = new List<BasePacket>();
        public List<IBaseEvent> EventsInClientLogic { get; private set; } = new List<IBaseEvent>();
        public List<IClientEvent> EventsInSdk { get; private set; } = new List<IClientEvent>();
        public TestGameClient() : base()
        {
            Network = base.Network as ClientNetwork;
            Network.OnReceiveGenericPacket += OnReceivePacket;
            this.ClientEvents.Register<GameStartedEvent>(this, OnGameStart);
        }

        /// <summary>
        /// When game starts we hook to game events for testing purposes
        /// </summary>
        private void OnGameStart(GameStartedEvent ev)
        {
            ClientEvents.OnEventFired += e => EventsInSdk.Add(e.ShallowClone());
            ev.Game.Events.OnEventFired += e => EventsInClientLogic.Add(e.ShallowClone());
        }

        public void PrepareSDK()
        {
            GameId.DEBUG_MODE = 1;
            Modules.Views.RegisterView<TileEntity, EntityView<TileEntity>>();
            Modules.Views.RegisterView<PartyEntity, EntityView<PartyEntity>>();
            Modules.Views.RegisterView<DungeonEntity, EntityView<DungeonEntity>>();
            Modules.Views.RegisterView<PlayerBuildingEntity, EntityView<PlayerBuildingEntity>>();
        }

        private void OnReceivePacket(BasePacket packet)
        {
            if(!(packet is TileUpdatePacket)) Game?.Log?.Debug($"Received {packet} from server ");
            ReceivedPackets.Add(packet);
        }

        public T GetLatestPacket<T>() where T : BasePacket 
        {
            return (T)ReceivedPackets.First(p => p.GetType() == typeof(T));
        }

        public async Task<T> WaitFor<T>(Func<T, bool> validate = null, int timeout = 20) where T : BasePacket
        {
            Network.Tick();
            var p = ReceivedPackets.FirstOrDefault(p => p.GetType() == typeof(T));
            while (p == null && timeout >= 0)
            {
                timeout--;
                await Task.Delay(100);
                Network.Tick();
                p = ReceivedPackets.FirstOrDefault(p => p.GetType() == typeof(T) && (validate==null || validate((T)p)));
            }
            if (timeout == 0) p = null;
            Network.Tick();
            return (T)p;
        }

        public void Dispose()
        {
           UnmanagedMemory.FreeAll();
        }
    }
}
