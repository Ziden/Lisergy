using Game.World;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Game.Engine.DataTypes
{
    /// <summary>
    /// Simpler to serialize structure of Guids.
    /// Guarantees uniqueness using Guids. It uses 16 bytes like guids
    /// Main difference is that its an unmanaged data structure that's serialized as two ulongs
    /// for faster serialization, reading and writing
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public unsafe struct GameId : IEquatable<GameId>, IEqualityComparer<GameId>
    {
        /// <summary>
        /// Hack Just for testing. Makes generation incremental and ToString will print number instead of guid.
        /// 0 = disabled.
        /// This is just during initial development later can think a better solution for debugging
        /// </summary>
        public static ulong DEBUG_MODE = 0;

        /// <summary>
        /// Sets whats to be the next generated game id
        /// </summary>
        public static GameId NextGeneration;

        public static GameId ZERO = Guid.Empty;

        /// <summary>
        /// Actual data of the game id is 2 ulongs (16 bytes)
        /// </summary>
        public ulong _leftside;
        public ulong _rightside;

        public byte[] GetBytes()
        {
            var bytes = new byte[16];
            fixed (byte* pointer = bytes)
            {
                *(ulong*)pointer = _leftside;
                *(ulong*)(pointer + 8) = _rightside;
            }
            return bytes;
        }

        public static GameId Generate()
        {

            if (NextGeneration != ZERO)
            {
                var val = NextGeneration;
                NextGeneration = ZERO;
                return val;
            }

            if (DEBUG_MODE > 0)
            {
                DEBUG_MODE++;
                return new GameId() { _leftside = 0, _rightside = DEBUG_MODE };
            }
            return Guid.NewGuid();
        }

        public static implicit operator GameId(Guid id)
        {
            var bytes = id.ToByteArray();
            fixed (byte* pointer = bytes)
            {
                return new GameId()
                {
                    _leftside = *(ulong*)pointer,
                    _rightside = *(ulong*)(pointer + 8)
                };
            }
        }

        public bool IsZero()
        {
            return this == ZERO;
        }

        public static implicit operator Guid(GameId id)
        {
            return new Guid(id.GetBytes());
        }


        public static bool operator !=(GameId g1, Guid g2)
        {
            return !g1.IsEqualsTo(g2);
        }

        public static bool operator ==(GameId g1, Guid g2)
        {
            return g1.IsEqualsTo(g2);
        }

        public static bool operator !=(GameId g1, GameId g2)
        {
            return !g1.IsEqualsTo(g2);
        }

        public static bool operator ==(GameId g1, GameId g2)
        {
            return g1.IsEqualsTo(g2);
        }

        public bool Equals(GameId obj)
        {
            return IsEqualsTo(obj);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString()
        {
            if (DEBUG_MODE > 0) return _rightside.ToString();
            if (IsZero())
            {
                return Guid.Empty.ToString();
            }

            Guid g = this;
            return g.ToString();
        }

        public unsafe bool IsEqualsTo(GameId id2)
        {
            return _leftside == id2._leftside && _rightside == id2._rightside;
        }

        public Location ToPosition()
        {
            return new Location()
            {
                X = (ushort)_leftside,
                Y = (ushort)_rightside
            };
        }

        public GameId(Location pos)
        {
            _leftside = pos.X;
            _rightside = pos.Y;
        }

        public GameId(ulong l, ulong l2)
        {
            _leftside = l;
            _rightside = l2;
        }

        public override unsafe int GetHashCode()
        {
            return HashCode.Combine(_leftside, _rightside);
        }

        public bool Equals(GameId x, GameId y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(GameId obj)
        {
            return obj.GetHashCode();
        }
    }
}
