﻿using Game.Engine.DataTypes;
using Game.Network.ServerPackets;
using System;
using System.Runtime.InteropServices;

namespace Game.Systems.Battle.Data
{
    /// <summary>
    /// Represents a minimal header of the battle.
    /// Contains the final state of the attacker and defender teams
    /// </summary>
    [Serializable]
    public class BattleHeader
    {
        public bool AttackerWins;
        public GameId BattleID;
        public BattleTeamData Attacker;
        public BattleTeamData Defender;
        public DateTime BattleTime;
    }
}
