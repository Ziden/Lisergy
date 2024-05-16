using Game.Engine.DataTypes;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnitTests")]
namespace Game.Engine.Scheduler
{
    /// <summary>
    /// Responsible for controlling the game tasks.
    /// </summary>
    public interface IGameScheduler
    {
        public DateTime Now { get; }
        public GameTask GetTask(GameId id);
        public void Add(GameTask task);
        void Cancel(GameTask task);
    }

    public class GameScheduler : IGameScheduler
    {
        private readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private Dictionary<GameId, GameTask> _tasks = new Dictionary<GameId, GameTask>();

        public DateTime Now { get; private set; }
        internal TimeSpan NowTimespan => Now - Epoch;
        internal GameTask NextTask { get; private set; }
        internal SortedSet<GameTask> Queue { get; private set; } = new SortedSet<GameTask>();
        internal void ForceComplete(GameTask task)
        {
            while (task.Repeat)
            {
                task.Tick();
            }
            task.Game.Entities.DeltaCompression.SendDeltaPackets(task.Creator);  // TODO: Maybe not best place
            Cancel(task);
        }


        public GameTask GetTask(GameId id)
        {
            _tasks.TryGetValue(id, out var task);
            return task;
        }

        public int PendingTasks => _tasks.Values.Count();
        public int AmountQueues => Queue.Count;

        internal void Clear()
        {
            Now = DateTime.MinValue;
            _tasks = new Dictionary<GameId, GameTask>();
            Queue = new SortedSet<GameTask>();
            NextTask = null;
        }

        internal void SetLogicalTime(DateTime time)
        {
            Now = time;
        }

        public void Cancel(GameTask task)
        {
            _ = _tasks.Remove(task.ID);
            _ = Queue.Remove(task);
            task.Delay = TimeSpan.FromSeconds(0);
            task.Dispose();
        }

        internal void RunTask(GameTask task)
        {
            task.Tick();
            _ = Queue.Remove(task);
            _ = _tasks.Remove(task.ID);
            task.Game.Entities.DeltaCompression.SendDeltaPackets(task.Creator); // TODO: Maybe not best place
            if (task.Repeat)
            {
                task.Start = Now;
                Add(task);
            }
            else
            {
                task.Dispose();
            }
            NextTask = Queue.FirstOrDefault();
        }

        public void Tick(DateTime time)
        {
            SetLogicalTime(time);
            _ = Now;
            if (NextTask == null)
            {
                NextTask = Queue.FirstOrDefault();
            }

            while (NextTask != null && NextTask.IsDue())
            {
                RunTask(NextTask);
            }
        }

        public void Add(GameTask task)
        {
            task.Start = Now;
            _tasks[task.ID] = task;
            _ = Queue.Add(task);
        }
    }
}
