using Game.Engine.ECS;
using Game.Systems.Party;

namespace Game.Systems.Resources
{
    public unsafe class ActionPointsLogic : BaseEntityLogic<ActionPointsComponent>
	{
		public int GetActionPoints()
		{
			return Entity.Get<ActionPointsComponent>().ActionPoints;
		}

		public void SetActionPoints(byte amt)
		{
			var c = Entity.Components.GetPointer<ActionPointsComponent>();
			c->ActionPoints = amt;
		}
	}
}