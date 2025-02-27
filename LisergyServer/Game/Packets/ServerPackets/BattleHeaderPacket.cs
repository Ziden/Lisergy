﻿using Game.Engine.Network;
using Game.Systems.Battle.Data;
using System;

namespace Game.Events
{
    /// <summary>
    /// Summary of the battle result
    /// </summary>
    [Serializable]
    public class BattleHeaderPacket : BasePacket, IServerPacket
    {
        public BattleHeader BattleHeader;

        public BattleHeaderPacket(BattleHeader header)
        {
            BattleHeader = header;
        }
    }
}
