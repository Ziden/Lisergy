using Game;
using Game.ECS;
using Game.Engine.ECS;
using Game.Events.ServerEvents;
using Game.Systems.Dungeon;
using Game.Systems.FogOfWar;
using Game.Systems.Map;
using Game.Systems.Party;
using NUnit.Framework;
using ServerTests;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace UnitTests
{
    public unsafe class TestECS
    {
        [StructLayout(LayoutKind.Sequential)]
        [SyncedComponent]
        internal struct SimpleComponent : IComponent
        {
            public int PublicField;
            public int Property { get; set; }
        }

        [StructLayout(LayoutKind.Sequential)]
        [SyncedComponent(OnlyMine = false)]
        internal struct PublicSyncComponent : IComponent
        {
            public int Property { get; set; }
        }

        [StructLayout(LayoutKind.Sequential)]
        [SyncedComponent(OnlyMine=true)]
        internal struct SelfSyncComponent : IComponent
        {
            public int Property { get; set; }
        }

        [Test]
        public void TestRegistrationByType()
        {
            var components = new ComponentSet(new DungeonEntity(null));
            IComponent toAdd = new EntityVisionComponent();
            components.Add<EntityVisionComponent>();

            Assert.AreEqual(toAdd, components.Get<EntityVisionComponent>());
        }

        [Test]
        public void TestGenericAdd()
        {
            var components = new ComponentSet(new DungeonEntity(null));
            IComponent toAdd = new EntityVisionComponent();
            components.Add<EntityVisionComponent>();

            Assert.AreEqual(toAdd, components.Get<EntityVisionComponent>());
        }

        [Test]
        public void TestDeltaTrackedOnAdd()
        {
            var player = new TestServerPlayer(new TestGame());
            var clientEntity = new PartyEntity(player.Game, player.EntityId);
            clientEntity.Components.Add<SelfSyncComponent>();
            var componentSet = clientEntity.Components as ComponentSet;

            Assert.IsTrue(clientEntity.Components.GetComponentDeltas(player).updated.Contains(clientEntity.Components.Get<SelfSyncComponent>()));

            var selfPacket = clientEntity.GetUpdatePacket(player) as EntityUpdatePacket;

            Assert.IsTrue(selfPacket.SyncedComponents.Contains(clientEntity.Components.Get<SelfSyncComponent>()));
        }

        [Test]
        public void TestSyncOnlyMine()
        {
            var player = new TestServerPlayer(new TestGame());
            var clientEntity = new PartyEntity(player.Game, player.EntityId);
            clientEntity.Components.Add<SelfSyncComponent>();

            var selfComponent = clientEntity.Components.Get<SelfSyncComponent>();
            clientEntity.Components.Add<PublicSyncComponent>();
            var publicComponent = clientEntity.Get<PublicSyncComponent>();

            var selfPacket = clientEntity.GetUpdatePacket(player) as EntityUpdatePacket;

            Assert.IsTrue(selfPacket.SyncedComponents.Contains(selfComponent));
            Assert.IsTrue(selfPacket.SyncedComponents.Contains(publicComponent));
        }

        [Test]
        public void TestSyncOnlyPublic()
        {
            var player = new TestServerPlayer(new TestGame());
            var clientEntity = new PartyEntity(player.Game, player.EntityId);
            clientEntity.Components.Add<SelfSyncComponent>();

            var selfComponent = clientEntity.Components.Get<SelfSyncComponent>();
            clientEntity.Components.Add<PublicSyncComponent>();
            var publicComponent = clientEntity.Get<PublicSyncComponent>();

            var publicPacket = clientEntity.GetUpdatePacket(null) as EntityUpdatePacket;

            Assert.IsTrue(!publicPacket.SyncedComponents.Contains(selfComponent));
            Assert.IsTrue(publicPacket.SyncedComponents.Contains(publicComponent));
        }

        public class TestView : IComponent
        {
            public static EntityMoveOutEvent called = default;

            public static void Callback(TestView view, EntityMoveOutEvent ev)
            {
                TestView.called = ev;
            }
        }

        [Test]
        public void TestSimpleModify()
        {
            var player = new TestServerPlayer(new TestGame());
            var clientEntity = new PartyEntity(player.Game, player.EntityId);
            clientEntity.Components.Add<SelfSyncComponent>();
            var component1 = clientEntity.Get<SelfSyncComponent>();
            component1.Property = 666;
            clientEntity.Save(component1);

            var component2 = clientEntity.Get<SelfSyncComponent>();
           
            Assert.AreEqual(666, component2.Property);
        }

        [Test]
        public void TestComponentNoModifyIfNoSave()
        {
            var player = new TestServerPlayer(new TestGame());
            var clientEntity = new PartyEntity(player.Game, player.EntityId);
            clientEntity.Components.Add<SelfSyncComponent>();
            var component1 = clientEntity.Get<SelfSyncComponent>();
            component1.Property = 666;
            clientEntity.Save(component1);

            var component2 = clientEntity.Get<SelfSyncComponent>();
            component2.Property = 123;

            var component3 = clientEntity.Get<SelfSyncComponent>();


            Assert.AreEqual(666, component3.Property);
            Assert.AreEqual(123, component2.Property);
        }

        [Test]
        public void TestComponentModifiesAfterSave()
        {
            var player = new TestServerPlayer(new TestGame());
            var clientEntity = new PartyEntity(player.Game, player.EntityId);
            clientEntity.Components.Add<SelfSyncComponent>();
            var component1 = clientEntity.Get<SelfSyncComponent>();
            component1.Property = 666;
            clientEntity.Save(component1);

            var component2 = clientEntity.Get<SelfSyncComponent>();
            component2.Property = 123;
            clientEntity.Save(component2);

            var component3 = clientEntity.Get<SelfSyncComponent>();

            Assert.AreEqual(123, component2.Property);
            Assert.AreEqual(123, component3.Property);
        }

        [StructLayout(LayoutKind.Sequential)]
        [SyncedComponent(OnlyMine = true)]
        internal struct SelfSyncComponentMagicTracker : IComponent
        {
            private int _v;
            public int Property { get => _v; set => _v = value; }
        }
    }
}