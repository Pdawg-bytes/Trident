using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Trident.Core.Debugging.Breakpoints
{
    public sealed class BreakpointManager(int maxBreakpoints = 64)
    {
        private uint? _suppressOnce;
        private readonly HashSet<uint> _breakpoints = [];
        private readonly ConcurrentQueue<uint> _hitQueue = new();
        private readonly int _maxBreakpoints = maxBreakpoints;

        public bool Enabled { get; private set; }
        public int Count => _breakpoints.Count;
        public int MaxBreakpoints => _maxBreakpoints;


        public bool Add(uint address)
        {
            if (_breakpoints.Count >= _maxBreakpoints)
                return false;

            _breakpoints.Add(address);
            Enabled = true;
            return true;
        }

        public void Remove(uint address)
        {
            _breakpoints.Remove(address);
            if (_breakpoints.Count == 0)
                Enabled = false;
        }

        public void Clear()
        {
            _breakpoints.Clear();
            Enabled = false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBreakpoint(uint pc)
        {
            if (_suppressOnce.HasValue && _suppressOnce.Value == pc)
            {
                _suppressOnce = null;
                return false;
            }

            if (_breakpoints.Contains(pc))
            {
                _hitQueue.Enqueue(pc);
                return true;
            }
            return false;
        }


        public bool TryDequeueHit(out uint addr) => _hitQueue.TryDequeue(out addr);
        public bool HasHits => !_hitQueue.IsEmpty;


        public int CopyTo(Span<uint> destination)
        {
            int i = 0;
            foreach (var bp in _breakpoints)
            {
                if (i >= destination.Length) break;
                destination[i++] = bp;
            }
            return i;
        }


        public void Continue(uint addr)
        {
            if (_breakpoints.Contains(addr))
                _suppressOnce = addr;
        }
    }
}