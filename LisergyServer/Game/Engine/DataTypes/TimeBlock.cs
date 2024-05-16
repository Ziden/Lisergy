using System;

namespace Game.Engine.DataTypes
{
    /// <summary>
    /// Represents the time a task is due executing
    /// </summary>
    public struct TimeBlock
    {
        /// <summary>
        /// Time the task has started
        /// </summary>
        public DateTime StartTime;

        /// <summary>
        /// Time the task is suposed to end
        /// </summary>
        public DateTime EndTime;

        /// <summary>
        /// Gets the total time required for this task
        /// </summary>
        public TimeSpan TotalBlockTime => EndTime - StartTime;

        public override string ToString()
        {
            return $"<TimeBlock {StartTime} to {EndTime}>";
        }
    }

    /// <summary>
    /// Snapshot of the current situation of a given time block, given time interpolations.
    /// This is to help with harvesting predictions.
    /// </summary>
    public unsafe struct TimeBlockSnapshot
    {
        /// <summary>
        /// Time the task is to be executed
        /// </summary>
        public TimeBlock TimeBlock;

        /// <summary>
        /// The total time the entity already spent on the task
        /// </summary>
        public TimeSpan TimeInBlock;

        /// <summary>
        /// Amount of time remaining to finish the task
        /// </summary>
        public TimeSpan TimeUntilEndOfblock;

        /// <summary>
        /// From 1.0 to 0.0 how much of this time block has elapsed ?
        /// </summary>
        public double Percentagage;
    }

    /// <summary>
    /// Helpers to manipulate and interpolate time blocks
    /// </summary>
    public static class TimeBlockUtils
    {
        /// <summary>
        /// Gets the timeblock from the current date up to the duration
        /// </summary>
        public static TimeBlock GetTimeBlock(this DateTime start, TimeSpan duration)
        {
            return new TimeBlock() { StartTime = start, EndTime = start + duration };
        }

        /// <summary>
        /// Gets a predicted current snapshot of the timeblock
        /// </summary>
        public static TimeBlockSnapshot GetCurrentSnapshot(this TimeBlock block, DateTime now)
        {
            var snap = new TimeBlockSnapshot();
            snap.TimeInBlock = now - block.StartTime;
            snap.TimeUntilEndOfblock = block.EndTime - now;
            snap.TimeBlock = block;
            snap.Percentagage = Math.Min(1, snap.TimeInBlock / block.TotalBlockTime);
            return snap;
        }
    }
}
