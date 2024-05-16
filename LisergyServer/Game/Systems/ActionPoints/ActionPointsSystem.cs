using Game.Engine.ECS;
using Game.Engine.Events.Bus;
using Game.Systems.Course;
using Game.Systems.Map;
using Game.Systems.Resources;

namespace Game.Systems.Party
{
	public class ActionsPointSystem : LogicSystem<ActionPointsComponent, ActionPointsLogic>, IEventListener
	{
		public ActionsPointSystem(LisergyGame game) : base(game)
		{
		}

		public override void RegisterListeners()
		{
			EntityEvents.On<CourseIntentionEvent>(OnCourseIntent);
			EntityEvents.On<EntityMoveInEvent>(OnEntityMoveIn);
		}

		private void OnEntityMoveIn(IEntity e, EntityMoveInEvent ev)
		{
			var actionPoints = ev.Entity.EntityLogic.ActionPoints.GetActionPoints();
			if (actionPoints == 0) return;
			ev.Entity.EntityLogic.ActionPoints.SetActionPoints((byte) (actionPoints - 1));
		}

		private void OnCourseIntent(IEntity e, CourseIntentionEvent ev)
		{
			if (ev.Entity.EntityLogic.ActionPoints.GetActionPoints() == 0)
			{
				ev.Cancelled = true;
			}
		}
	}
}