namespace Trident.Core.Scheduling
{
    internal sealed class SchedulerEvent(ulong timestamp, uint priority, ulong uid, ulong ctx, EventType eventType) : IComparable<SchedulerEvent>
    {
        internal readonly ulong Timestamp = timestamp;
        internal readonly uint Priority = priority;
        internal readonly ulong UniqueID = uid;
        internal readonly ulong Context = ctx;
        internal readonly EventType EventType = eventType;


        public int CompareTo(SchedulerEvent? other)
        {
            if (other is null) return 1;

            int timestamp = Timestamp.CompareTo(other.Timestamp);
            if (timestamp != 0) return timestamp;

            int priority = Priority.CompareTo(other.Priority);
            if (priority != 0) return priority;

            return UniqueID.CompareTo(other.UniqueID);
        }
    }
}