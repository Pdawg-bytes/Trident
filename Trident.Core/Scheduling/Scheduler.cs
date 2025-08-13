using System.Diagnostics;

namespace Trident.Core.Scheduling
{
    public class Scheduler
    {
        private readonly SortedSet<SchedulerEvent> _events = [];
        private readonly Dictionary<ulong, SchedulerEvent> _uidEventMap = [];
        private readonly Dictionary<EventType, Action<ulong>> _callbacks = [];

        private ulong _nextId = 1;

        internal ulong CurrentTimestamp { get; set; }
        internal ulong NextTimestamp => _events.Count > 0 ? _events.Min!.Timestamp : ulong.MaxValue;
        internal ulong RemainingCycles => NextTimestamp - CurrentTimestamp;

        public Scheduler()
        {
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
            _events.Clear();
            _uidEventMap.Clear();
            CurrentTimestamp = 0;
            _nextId = 1;

            Schedule(EventType.EndOfQueue, ulong.MaxValue);
        }

        internal void Register(EventType eventType, Action callback) => _callbacks[eventType] = _ => callback();
        internal void Register(EventType eventType, Action<ulong> callback) => _callbacks[eventType] = callback;
        internal SchedulerEvent? EventByUID(ulong uid) => _uidEventMap.GetValueOrDefault(uid);


        internal void Step(ulong cycles)
        {
            ulong timestampNext = CurrentTimestamp + cycles;

            while (_events.Count > 0 && _events.Min!.Timestamp <= timestampNext)
            {
                var evt = _events.Min!;

                CurrentTimestamp = evt.Timestamp;
                _callbacks[evt.EventType](evt.Context);
                Remove(evt);
            }

            CurrentTimestamp = timestampNext;
        }


        internal SchedulerEvent Schedule(EventType eventType, ulong delay, uint priority = 0, ulong ctx = 0)
        {
            SchedulerEvent evt = new
            (
                timestamp: CurrentTimestamp + delay,
                priority: priority,
                uid: _nextId++,
                ctx: ctx,
                eventType: eventType
            );

            _events.Add(evt);
            _uidEventMap.Add(evt.UniqueID, evt);

            return evt;
        }

        internal void Remove(SchedulerEvent? evt)
        {
            if (evt is not null)
            {
                _events.Remove(evt);
                _uidEventMap.Remove(evt.UniqueID);
            }
        }

        internal void Remove(ulong uid) => Remove(_events.Where(evt => evt.UniqueID == uid).FirstOrDefault());
    }
}