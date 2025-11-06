using System.Diagnostics;

namespace Trident.Core.Scheduling
{
    public class Scheduler
    {
        private const int DefaultCapacity = 64;
        private readonly SchedulerHeap _heap;
        private readonly Dictionary<EventType, Action<ulong>> _callbacks = [];

        private ulong _nextId = 1;

        internal ulong CurrentTimestamp { get; private set; }
        internal ulong NextTimestamp => _heap.Count > 0 ? _heap.Min.Timestamp : ulong.MaxValue;
        internal ulong CyclesToNextEvent => NextTimestamp - CurrentTimestamp;

        public Scheduler(int capacity = DefaultCapacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            _heap = new SchedulerHeap(capacity);

            foreach (EventType type in Enum.GetValues(typeof(EventType)))
            {
                if (type == EventType.Count) continue;
                _callbacks[type] = (data) => Debug.Assert(false, $"Scheduler: unhandled event type: {type}");
            }

            Register(EventType.EndOfQueue, () => Debug.Assert(false, "Scheduler: reached end of event queue"));

            Reset();
        }

        internal void Reset()
        {
            _heap.Clear();
            CurrentTimestamp = 0;
            _nextId = 1;

            #if DEBUG
            Schedule(EventType.EndOfQueue, ulong.MaxValue);
            #endif
        }

        internal void Register(EventType eventType, Action callback) => _callbacks[eventType] = _ => callback();
        internal void Register(EventType eventType, Action<ulong> callback) => _callbacks[eventType] = callback;

        internal SchedulerEvent? EventByUID(ulong uid)
        {
            for (int i = 0; i < _heap.Count; i++)
                if (_heap.GetAt(i).UniqueID == uid)
                    return _heap.GetAt(i);

            return null;
        }


        internal void Step(uint cycles) => Step((ulong)cycles);
        internal void Step(ulong cycles)
        {
            ulong timestampNext = CurrentTimestamp + cycles;

            while (_heap.Count > 0)
            {
                SchedulerEvent nextEvent = _heap.Min;
                if (nextEvent.Timestamp > timestampNext)
                    break;

                CurrentTimestamp = nextEvent.Timestamp;

                _heap.Pop();
                _callbacks[nextEvent.EventType](nextEvent.Context);
            }

            CurrentTimestamp = timestampNext;
        }

        internal void SkipToNextEvent() => Step(NextTimestamp - CurrentTimestamp);


        internal void Schedule(EventType eventType, ulong delay, uint priority = 0, ulong ctx = 0)
        {
            var evt = new SchedulerEvent(
                timestamp: CurrentTimestamp + delay,
                priority: priority,
                uid: _nextId++,
                ctx: ctx,
                eventType: eventType
            );

            _heap.Insert(evt);
        }

        internal void Remove(SchedulerEvent evt) => Remove(evt.UniqueID);
        internal void Remove(ulong uid)
        {
            for (int i = 0; i < _heap.Count; i++)
            {
                if (_heap.GetAt(i).UniqueID == uid)
                {
                    _heap.RemoveAt(i);
                    return;
                }
            }
        }
    }
}