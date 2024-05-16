using Game.Engine.DataTypes;
using Game.Systems.Battle;
using Game.Systems.Battle.Data;
using Game.Systems.Battler;
using System;
using System.Linq;

namespace ServerTests
{
    public class TestBattle : TurnBattle
    {
        public TestBattle(in BattleTeamData t1, in BattleTeamData t2) : base(GameId.Generate(), t1, t2) { }

        public BattleUnit NextUnitToAct => _actionQueue.First();

        public TurnBattleRecord Log => Record;

        public static ref Unit MakeOverpower(ref Unit u)
        {
            u.Atk = 200;
            u.Speed = 200;
            u.Def = 200;
            u.HP = 200;
            u.MaxHP = 200;
            return ref u;
        }
    }
}
