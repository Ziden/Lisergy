using Game;
using Game.Engine;
using Game.Engine.Scheduler;
using NUnit.Framework;
using ServerTests;
using System;
using System.Linq;

namespace UnitTests
{
    public class TestScheduler
    {
        private TestGame _game;
        private GameScheduler _scheduler;

        private class TestTask : GameTask
        {
            public TestTask(IGame game, TimeSpan delay) : base(game, delay, null, null)
            {
                game.Scheduler.Add(this);
            }
            public bool Ran = false;
            public override void Tick()
            {
                if (Ran)
                    throw new Exception("Tryng to run task twice");
                Ran = true;
            }
        }

        [SetUp]
        public void Setup()
        {
            _game = new TestGame();
            _scheduler = _game.GameScheduler;
            _scheduler.SetLogicalTime(DateTime.UtcNow);
        }

        [Test]
        public void TestOrdering()
        {
            _scheduler.SetLogicalTime(DateTime.UnixEpoch);

            var t1 = new TestTask(_game, TimeSpan.FromSeconds(1));
            var t2 = new TestTask(_game, TimeSpan.FromSeconds(2));
            var t3 = new TestTask(_game, TimeSpan.FromSeconds(3));

            var minute = (long)_scheduler.NowTimespan.TotalMinutes;
            var queue = _scheduler.Queue;

            Assert.AreEqual(t1, queue.First());
            Assert.AreEqual(t3, queue.Last());
            Assert.AreEqual(3, _scheduler.PendingTasks);
        }

        [Test]
        public void TestTickNotRunningTasks()
        {
            var time = DateTime.UnixEpoch;
            _scheduler.SetLogicalTime(time);

            var t1 = new TestTask(_game, TimeSpan.FromSeconds(1));
            var t2 = new TestTask(_game, TimeSpan.FromSeconds(1 + 60));
            var t3 = new TestTask(_game, TimeSpan.FromSeconds(1 + 120));

            _scheduler.Tick(time);

            Assert.IsFalse(t1.Ran);
            Assert.IsFalse(t2.Ran);
            Assert.IsFalse(t3.Ran);
            Assert.AreEqual(t1, _scheduler.NextTask);
        }

        [Test]
        public void TestRunningTask()
        {
            var time = DateTime.UnixEpoch;
            _scheduler.SetLogicalTime(time);
            _scheduler.Tick(time);

            var t1 = new TestTask(_game, TimeSpan.FromSeconds(1));
            var t2 = new TestTask(_game, TimeSpan.FromSeconds(1 + 60));
            var t3 = new TestTask(_game, TimeSpan.FromSeconds(1 + 120));

            _scheduler.Tick(time + TimeSpan.FromMinutes(1));

            Assert.IsTrue(t1.Ran);
            Assert.IsFalse(t2.Ran);
            Assert.IsFalse(t3.Ran);
            Assert.AreEqual(t2, _scheduler.NextTask);
            Assert.AreEqual(2, _scheduler.PendingTasks);
        }
        
        [Test]
        public void TackingUnmanagedMemory()
        {
            var t1 = new TestTask(_game, TimeSpan.FromSeconds(1));
            var t2 = new TestTask(_game, TimeSpan.FromSeconds(5));
            var t3 = new TestTask(_game, TimeSpan.FromSeconds(7));

            Assert.AreNotEqual(t1.Pointer, t2.Pointer);
            Assert.AreNotEqual(t1.Pointer, t3.Pointer);
            
            Assert.IsTrue(UnmanagedMemory._allocs.ContainsKey(t1.Pointer));
            Assert.IsTrue(UnmanagedMemory._allocs.ContainsKey(t2.Pointer));
            Assert.IsTrue(UnmanagedMemory._allocs.ContainsKey(t3.Pointer));
            
            t1.Dispose();
            t2.Dispose();
            t3.Dispose();
            
            Assert.IsFalse(UnmanagedMemory._allocs.ContainsKey(t1.Pointer));
            Assert.IsFalse(UnmanagedMemory._allocs.ContainsKey(t2.Pointer));
            Assert.IsFalse(UnmanagedMemory._allocs.ContainsKey(t3.Pointer));
        }

        [Test]
        public void TestRunningTwoTasks()
        {
            var time = DateTime.UnixEpoch;
            _scheduler.SetLogicalTime(time);

            var t1 = new TestTask(_game, TimeSpan.FromSeconds(1));
            var t2 = new TestTask(_game, TimeSpan.FromSeconds(5));
            var t3 = new TestTask(_game, TimeSpan.FromSeconds(7));

            _scheduler.Tick(time + TimeSpan.FromSeconds(5));

            Assert.IsTrue(t1.Ran);
            Assert.IsTrue(t2.Ran);
            Assert.IsFalse(t3.Ran);
            Assert.AreEqual(t3, _scheduler.NextTask);
            Assert.AreEqual(1, _scheduler.PendingTasks);
        }

        [Test]
        public void TestRunningVeryOldTasks()
        {
            var time = DateTime.UnixEpoch;
            _scheduler.SetLogicalTime(time);

            var t1 = new TestTask(_game, TimeSpan.FromSeconds(1));
            var t2 = new TestTask(_game, TimeSpan.FromHours(12));
            var t3 = new TestTask(_game, TimeSpan.FromDays(2));

            _scheduler.Tick(time + TimeSpan.FromDays(1));

            Assert.IsTrue(t1.Ran);
            Assert.IsTrue(t2.Ran);
            Assert.IsFalse(t3.Ran);
            Assert.AreEqual(t3, _scheduler.NextTask);
            Assert.AreEqual(1, _scheduler.PendingTasks);
        }

        [Test]
        public void TestNextTask()
        {
            _scheduler.SetLogicalTime(DateTime.UnixEpoch);

            var t1 = new TestTask(_game, TimeSpan.FromSeconds(10));
            
            _scheduler.Tick(DateTime.UnixEpoch + TimeSpan.FromMilliseconds(1));

            Assert.AreEqual(t1, _scheduler.NextTask);
            Assert.AreEqual(1, _scheduler.PendingTasks);
        }
    }
}