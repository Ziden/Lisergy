using Game.ECS;
using System;

namespace Game.Systems.Party
{
    [Serializable]
    public struct ActionPointsComponent : IComponent
    {
        public byte ActionPoints;

        public override string ToString() => $"<ActionPointsComponent Points={ActionPoints}>";
    }
}
