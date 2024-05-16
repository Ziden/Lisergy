using Game.Engine;
using Game.Engine.DataTypes;
using Game.Events.ServerEvents;
using Game.Systems.Player;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests
{
    public class TestGameIds
    {
        [Test]
        public void CheckNotEqual()
        {
            var id1 = GameId.Generate();
            var id2 = GameId.Generate();

            Assert.AreNotEqual(id1, id2);
        }

        [Test]
        public void TestByteArrayComparisson()
        {
            var id1 = GameId.Generate();
            Guid id2 = id1;

            var arr1 = id1.GetBytes();
            var arr2 = id2.ToByteArray();
            Assert.True(arr1.SequenceEqual(arr2));
        }

        [Test]
        public void TestReserializationAsIndex()
        {
            Serialization.LoadSerializers();

            var p = new LoginResultPacket() { Profile = new PlayerProfile(GameId.Generate()) };

            var d = new Dictionary<GameId, int>();

            d[p.Profile.PlayerId] = 123;

            var p2 = Serialization.ToCastedPacket<LoginResultPacket>(Serialization.FromBasePacket(p));

            Assert.AreEqual(123, d[p2.Profile.PlayerId]);
        }

        [Test]
        public void CheckNotNullComparisson()
        {
            GameId nu = GameId.Generate();

            Assert.IsTrue(nu != null);
        }


        [Test]
        public void TestGuidBackForth()
        {
            var guid = GameId.Generate();
            GameId id1 = guid;
            Guid back = id1;


            Assert.AreEqual(guid.GetBytes(), back.ToByteArray());
        }

        public class TestClass
        {
            public GameId Zero;
        }

        [Test]
        public void TestUnassigned()
        {
            var c = new TestClass();

            Assert.IsTrue(c.Zero == Guid.Empty);
        }

        [Test]
        public void TestZero()
        {
            var guid = Guid.Empty;
            GameId id1 = guid;


            Assert.IsTrue(id1.IsZero());
        }

        [Test]
        public void TestZeroAssignments()
        {
            GameId zero1 = Guid.Empty;
            GameId zero2 = Guid.Empty;
            zero2 = zero1;
            Assert.IsTrue(zero1 == zero2);
        }


        [Test]
        public void TestZeroInit()
        {
            GameId zero = Guid.Empty;

            Assert.IsTrue(zero.IsZero());
        }

        [Test]
        public void TestDebugMode()
        {
            GameId.DEBUG_MODE = 1;
            GameId first = GameId.Generate();

            Assert.AreEqual(0, first._leftside);
            Assert.AreEqual(2, first._rightside);
            Assert.AreEqual("2", first.ToString());
        }

        [Test]
        public void TestNonInitialized()
        {
            GameId zero = default;

            Assert.IsTrue(zero.IsZero());
        }

        [Test]
        public void CheckEqual()
        {
            var id1 = GameId.Generate();
            var id2 = id1;

            Assert.IsTrue(id1 == id2);
        }

        [Test]
        public void CheckEqualOperator()
        {
            var guid = GameId.Generate();
            GameId id1 = guid;
            GameId id2 = guid;

            Assert.IsTrue(id1.IsEqualsTo(id2));
        }

        [Test]
        public void CheckNotEqualOperator()
        {
            GameId id1 = GameId.Generate();
            GameId id2 = GameId.Generate();

            Assert.IsTrue(id1 != id2);
        }
    }
}
