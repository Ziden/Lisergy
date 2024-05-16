using Game.Scheduler;
using Game.World;
using GameDataTest;
using NUnit.Framework;
using ServerTests;
using System;
using System.Linq;

namespace Tests
{
    public class TestFastScheduler
    {
        private class TestTask : FastGameTask
        {
            public TestTask(TimeSpan delay) : base(delay) {}
            public bool Ran = false;
            public override void Execute()
            {
                if (Ran)
                    throw new Exception("Tryng to run task twice");
                Ran = true;
            }
        }

        [SetUp]
        public void Setup()
        {
            FastScheduler.SetLogicalTime(DateTime.UtcNow);
            FastScheduler.Clear();
        }

        [Test]
        public void TestOrdering()
        {
            FastScheduler.SetLogicalTime(DateTime.UnixEpoch);

            var t1 = new TestTask(TimeSpan.FromSeconds(1));
            var t2 = new TestTask(TimeSpan.FromSeconds(2));
            var t3 = new TestTask(TimeSpan.FromSeconds(3));

            var minute = (long)FastScheduler.NowTimespan.TotalMinutes;
            var queue = FastScheduler.GetMinuteQueue(minute);

            Assert.AreEqual(t1, queue.First());
            Assert.AreEqual(t3, queue.Last());
            Assert.AreEqual(3, FastScheduler.PendingTasks);
        }

        [Test]
        public void TestMinuteQueues()
        {
            var time = DateTime.UnixEpoch;
            FastScheduler.SetLogicalTime(time);

            var t1 = new TestTask(TimeSpan.FromSeconds(1));
            var t2 = new TestTask(TimeSpan.FromSeconds(1 + 60));
            var t3 = new TestTask(TimeSpan.FromSeconds(1 + 120));

            var minute = (long)FastScheduler.NowTimespan.TotalMinutes;

            Assert.IsTrue(FastScheduler.GetMinuteQueue(minute).Contains(t1));
            Assert.IsTrue(FastScheduler.GetMinuteQueue(minute + 1).Contains(t2));
            Assert.IsTrue(FastScheduler.GetMinuteQueue(minute + 2).Contains(t3));
        }

        [Test]
        public void TestTickNotRunningTasks()
        {
            var time = DateTime.UnixEpoch;
            FastScheduler.SetLogicalTime(time);

            var t1 = new TestTask(TimeSpan.FromSeconds(1));
            var t2 = new TestTask(TimeSpan.FromSeconds(1 + 60));
            var t3 = new TestTask(TimeSpan.FromSeconds(1 + 120));

            FastScheduler.Tick(time);

            Assert.IsFalse(t1.Ran);
            Assert.IsFalse(t2.Ran);
            Assert.IsFalse(t3.Ran);
            Assert.AreEqual(t1, FastScheduler.NextTask);
        }

        [Test]
        public void TestRunningTask()
        {
            var time = DateTime.UnixEpoch;
            FastScheduler.SetLogicalTime(time);
            FastScheduler.Tick(time);

            var t1 = new TestTask(TimeSpan.FromSeconds(1));
            var t2 = new TestTask(TimeSpan.FromSeconds(1 + 60));
            var t3 = new TestTask(TimeSpan.FromSeconds(1 + 120));

            FastScheduler.Tick(time + TimeSpan.FromMinutes(1));

            Assert.IsTrue(t1.Ran);
            Assert.IsFalse(t2.Ran);
            Assert.IsFalse(t3.Ran);
            Assert.AreEqual(t2, FastScheduler.NextTask);
            Assert.AreEqual(2, FastScheduler.PendingTasks);
        }

        [Test]
        public void TestRunningTwoTasks()
        {
            var time = DateTime.UnixEpoch;
            FastScheduler.SetLogicalTime(time);

            var t1 = new TestTask(TimeSpan.FromSeconds(1));
            var t2 = new TestTask(TimeSpan.FromSeconds(5));
            var t3 = new TestTask(TimeSpan.FromSeconds(7));

            FastScheduler.Tick(time + TimeSpan.FromSeconds(5));

            Assert.IsTrue(t1.Ran);
            Assert.IsTrue(t2.Ran);
            Assert.IsFalse(t3.Ran);
            Assert.AreEqual(t3, FastScheduler.NextTask);
            Assert.AreEqual(1, FastScheduler.PendingTasks);
        }

        [Test]
        public void TestRunningVeryOldTasks()
        {
            var time = DateTime.UnixEpoch;
            FastScheduler.SetLogicalTime(time);

            var t1 = new TestTask(TimeSpan.FromSeconds(1));
            var t2 = new TestTask(TimeSpan.FromHours(12));
            var t3 = new TestTask(TimeSpan.FromDays(2));

            FastScheduler.Tick(time + TimeSpan.FromDays(1));

            Assert.IsTrue(t1.Ran);
            Assert.IsTrue(t2.Ran);
            Assert.IsFalse(t3.Ran);
            Assert.AreEqual(null, FastScheduler.NextTask);
            Assert.AreEqual(1, FastScheduler.PendingTasks);
        }

        [Test]
        public void TestNextTask()
        {
            FastScheduler.SetLogicalTime(DateTime.UnixEpoch);

            var t1 = new TestTask(TimeSpan.FromSeconds(10));

            FastScheduler.Tick(DateTime.UnixEpoch + TimeSpan.FromMilliseconds(1));

            Assert.AreEqual(t1, FastScheduler.NextTask);
            Assert.AreEqual(1, FastScheduler.PendingTasks);
        }
    }
}