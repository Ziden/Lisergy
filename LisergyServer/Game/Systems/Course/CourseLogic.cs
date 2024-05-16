using System;
using System.Collections.Generic;
using Game.Systems.Course;
using Game.Tile;
using Game.Engine.Events;
using Game.Engine.ECS;
using Game.Engine.DataTypes;
using Game.Engine.Scheduler;
using Game.Systems.Party;
using Game.World;

namespace Game.Systems.Movement
{
    public unsafe class CourseLogic : BaseEntityLogic<CourseComponent>
    {
        public bool TryStartMovement(List<Location> sentPath, CourseIntent intent)
        {
            var owner = Game.Players.GetPlayer(Entity.OwnerID);

            var courseIntent = EventPool<CourseIntentionEvent>.Get();
            courseIntent.Intent = intent;
            courseIntent.Path = sentPath;
            courseIntent.Entity = Entity;
            courseIntent.Cancelled = false;
            Entity.Components.CallEvent(courseIntent);
            EventPool<CourseIntentionEvent>.Return(courseIntent);

            if (courseIntent.Cancelled)
            {
                Game.Log.Debug("Course Cancelled by other system");
                return false;
            }
            
            foreach (var position in sentPath)
            {
                var tile = Game.World.Map.GetTile(position.X, position.Y);
                if (!tile.Passable)
                {
                    Game.Log.Error($"Impassable TileEntity {tile} in course path: {owner} moving {Entity}");
                    return false;
                }
            }
            var movement = Entity.Components.GetPointer<CourseComponent>();
            var taskExecutor = new CourseTaskExecutor(Entity, sentPath, intent);
            var task = new GameTask(Game, TimeSpan.FromMilliseconds(1), owner, taskExecutor);
            Game.Scheduler.Add(task);
            movement->CourseId = task.ID;
            movement->MovementIntent = intent;
            var ev = EventPool<CourseStartEvent>.Get();
            ev.Entity = Entity;
            ev.Intent = movement->MovementIntent;
            Entity.Components.CallEvent(ev);
            EventPool<CourseStartEvent>.Return(ev);
            return true;
        }

        public void FinishCourse(TileEntity lastTile)
        {
            var movement = Entity.Components.GetPointer<CourseComponent>();
            movement->CourseId = GameId.ZERO;
            var ev = EventPool<CourseFinishEvent>.Get();
            ev.Entity = Entity;
            ev.Intent = movement->MovementIntent;
            ev.ToTile = lastTile;
            Entity.Components.CallEvent(ev);
            EventPool<CourseFinishEvent>.Return(ev);
        }

        public GameTask GetCourseTask() => Game.Scheduler.GetTask(Entity.Components.Get<CourseComponent>().CourseId);

        public GameTask? TryGetCourseTask()
        {
            var courseId = Entity.Components.Get<CourseComponent>().CourseId;
            if (courseId == GameId.ZERO) return null;
            return Game.Scheduler.GetTask(courseId);
        }

        public CourseTaskExecutor? TryGetCourseTaskExecutor()
        {
            return TryGetCourseTask()?.Executor as CourseTaskExecutor;
        }
    }
}
