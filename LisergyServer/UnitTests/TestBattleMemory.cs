using NUnit.Framework;
using Game.Systems.BattleGroup;
using Game.Systems.Battle.Data;

namespace UnitTests
{
    public unsafe class TestBattleMemory
    {
        private BattleTeamData TestData(byte leaderHP)
        {
            var data = new BattleTeamData();
            var group = new UnitGroup();
            var leader = group.Leader;
            leader.HP = leaderHP;
            group.Leader = leader;
            data.Units = group;
            return data;
        }

        [Test]
        public void TestMemoryInitialValue()
        {
            var data = TestData(123);

            var memory = new BattleTeamMemory(data);

            Assert.AreEqual(memory.GetUnit(0)->HP, data.Units.Leader.HP);
        }

        [Test]
        public void TestPointerModification()
        {
            var data = TestData(123);

            var memory = new BattleTeamMemory(data);
            memory.GetUnit(0)->HP = 200;
            memory.FreeAndCopyResults(ref data);

            Assert.AreEqual(data.Units.Leader.HP, 200);
        }

        [Test]
        public void TestReusingMemoryBuffrer()
        {
            var data = TestData(123);

            var initialMemory = new BattleTeamMemory(data);
            initialMemory.GetUnit(0)->HP = 200;
            initialMemory.FreeAndCopyResults(ref data);

            Assert.AreEqual(initialMemory.GetUnit(0)->HP, 200);
            var secondMemory = new BattleTeamMemory(TestData(60));
            Assert.AreEqual(initialMemory.GetUnit(0)->HP, 60);
            Assert.AreEqual(secondMemory.GetUnit(0)->HP, 60);
            Assert.AreEqual(initialMemory.Pointer, secondMemory.Pointer);
        }
    }
}
