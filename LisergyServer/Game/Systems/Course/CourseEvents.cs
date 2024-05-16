using System.Collections.Generic;
using Game.Engine.ECS;
using Game.Engine.Events;
using Game.Systems.Movement;
using Game.Tile;
using Game.World;

namespace Game.Systems.Course
{
    public class CourseIntentionEvent : IGameEvent
    {
        public IEntity Entity;
        public CourseIntent Intent;
        public List<Location> Path;
        public bool Cancelled;
        
        public override string ToString() => $"<CourseIntent Entity={Entity} Intent={Intent} Tile={Path}/>";
    }
    
    /// <summary>
    /// Whenever a course finishes its last tile movement
    /// </summary>
    public class CourseFinishEvent : IGameEvent
    {
        public IEntity Entity;
        public CourseIntent Intent;
        public TileEntity ToTile;

        public override string ToString() => $"<CourseFinish Entity={Entity} Intent={Intent} Tile={ToTile}/>";
    }

    /// <summary>
    /// Triggered when a course started
    /// </summary>
    public class CourseStartEvent : IGameEvent
    {
        public IEntity Entity;
        public CourseIntent Intent;

        public override string ToString() => $"<CourseStart Entity={Entity} Intent={Intent}/>";
    }

}
