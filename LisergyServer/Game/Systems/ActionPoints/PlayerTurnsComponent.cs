using System;
using Game.ECS;

namespace Game.Systems.ActionPoints
{
	[Serializable]
	public struct PlayerTurnsComponent : IComponent
	{
		public ulong Turns;
	}
}