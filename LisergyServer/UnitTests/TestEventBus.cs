using Game.Engine.Events.Bus;
using Game.World;
using NUnit.Framework;
using ServerTests;

namespace UnitTests
{
    public class TestEventBus : IEventListener
    {

        public class EventTest
        {

        }
       

        [Test]
        public void TestCallingEvent()
        {
            var bus = new EventBus<EventTest>();
            var called = false;
            bus.Register<EventTest>(this, e => called = true);

            bus.Call(new EventTest());

            Assert.IsTrue(called);
        }

        [Test]
        public void TestRemovingListener()
        {
            var bus = new EventBus<EventTest>();
            var called = false;
            bus.Register<EventTest>(this, e => called = true);
            bus.RemoveListener(this);
            bus.Call(new EventTest());

            Assert.IsFalse(called);
        }

    }
}