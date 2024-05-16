using Game;
using Game.Engine;
using Game.Engine.DataTypes;
using Game.Engine.Events;
using Game.Engine.Events.Bus;
using Game.Engine.Network;
using Game.Events.ServerEvents;
using Game.Network.ClientPackets;
using Game.Systems.Movement;
using Game.Systems.Party;
using Game.Systems.Player;
using Game.Tile;
using Game.World;
using LisergyServer.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerTests
{
    public class TestServerPlayer : PlayerEntity, IEventListener
    {
        public event Action<BasePacket> OnReceivedPacket;

        public List<BasePacket> ReceivedPackets = new List<BasePacket>();
        public List<IBaseEvent> TriggeredEvents = new List<IBaseEvent>();

        public bool IsOnline { get; set; }
        private GameServerNetwork _network;

        public TestServerPlayer(LisergyGame game) : base(new PlayerProfile(GameId.Generate()), game)
        {
            IsOnline = true;
            _network = game.Network as GameServerNetwork;
        }

        public void SendTestPacket<EventType>(EventType ev) where EventType : BasePacket, new()
        {
            if (ev.GetType() != typeof(TileUpdatePacket)) // avoid flood
            {
                Game.Log.Debug($"Server Sent Packet {ev.GetType().Name} to Player {this}");
            }
            var reSerialized = Serialization.ToBasePacket(Serialization.FromBasePacket(ev));
            PacketPool.Return(ev);
            OnReceivedPacket?.Invoke(reSerialized);
            ReceivedPackets.Add(reSerialized);
        }

        public void ListenTo<EventType>() where EventType : IBaseEvent
        {
            Game.Events.Register<EventType>(this, ev =>
            {
                TriggeredEvents.Add(ev.ShallowClone());
            });
        }

        public void SendMoveRequest(PartyEntity p, TileEntity t, CourseIntent intent)
        {
         
            var path = t.Chunk.Map.FindPath(p.Tile, t).Select(pa => new Location(pa.X, pa.Y)).ToList();
            p.EntityLogic.ActionPoints.SetActionPoints((byte)path.Count);
            var ev = new MoveRequestPacket() { Path = path, PartyIndex = p.PartyIndex, Intent = intent };
            ev.Sender = this;
            _network.IncomingPackets.Call(ev);
        }

        public List<T> ReceivedPacketsOfType<T>() where T : BasePacket
        {
            return ReceivedPackets.Where(e => e.GetType().IsAssignableFrom(typeof(T))).Select(e => e as T).ToList();
        }

        public List<T> TriggeredEventsOfType<T>() where T : IBaseEvent
        {
            return TriggeredEvents.Where(e => e.GetType().IsAssignableFrom(typeof(T))).Select(e => (T)e).ToList();
        }

        public override string ToString()
        {
            return $"<TestPlayer id={EntityId.ToString()}>";
        }
    }

}
