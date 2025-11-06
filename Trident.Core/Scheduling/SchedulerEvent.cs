namespace Trident.Core.Scheduling
{
    internal readonly struct SchedulerEvent(ulong timestamp, uint priority, ulong uid, ulong ctx, EventType eventType) : IComparable<SchedulerEvent>
    {
        public readonly ulong Timestamp = timestamp;
        public readonly uint Priority = priority;
        public readonly ulong UniqueID = uid;
        public readonly ulong Context = ctx;
        public readonly EventType EventType = eventType;

        public int CompareTo(SchedulerEvent other)
        {
            int ts = Timestamp.CompareTo(other.Timestamp);
            if (ts != 0) return ts;

            int pr = Priority.CompareTo(other.Priority);
            if (pr != 0) return pr;

            return UniqueID.CompareTo(other.UniqueID);
        }
    }
}