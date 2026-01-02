namespace Trident.Core.Scheduling;

internal class SchedulerHeap
{
    private SchedulerEvent[] _buffer;

    private int _count;
    internal int Count => _count;

    internal SchedulerHeap(int capacity)
    {
        _buffer = new SchedulerEvent[capacity];
        _count = 0;
    }

    internal SchedulerEvent Min
    {
        get
        {
            if (_count == 0) throw new InvalidOperationException("Heap is empty");
            return _buffer[0];
        }
    }

    internal SchedulerEvent GetAt(int index)
    {
        if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index));
        return _buffer[index];
    }


    internal SchedulerEvent Pop()
    {
        if (_count == 0)
            throw new InvalidOperationException("Heap is empty");

        SchedulerEvent result = _buffer[0];
        int lastIdx = --_count;

        if (lastIdx >= 0)
        {
            _buffer[0] = _buffer[lastIdx];
            HeapifyDown(0);
        }

        return result;
    }

    internal void Insert(SchedulerEvent value)
    {
        if (_count >= _buffer.Length)
            throw new InvalidOperationException($"Heap capacity {_buffer.Length} exceeded");

        int i = _count++;
        _buffer[i] = value;
        HeapifyUp(i);
    }

    internal void RemoveAt(int index)
    {
        if (index < 0 || index >= _count)
            return;

        int lastIdx = --_count;
        if (index == lastIdx)
            return;

        _buffer[index] = _buffer[lastIdx];
        HeapifyDown(index);
        HeapifyUp(index);
    }


    private void HeapifyUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (_buffer[parent].CompareTo(_buffer[i]) <= 0) break;

            Swap(parent, i);
            i = parent;
        }
    }

    private void HeapifyDown(int i)
    {
        while (true)
        {
            int left = (i << 1) + 1;
            if (left >= _count) break;

            int right = left + 1;
            int smallest = (right < _count && _buffer[right].CompareTo(_buffer[left]) < 0)
                ? right
                : left;

            if (_buffer[i].CompareTo(_buffer[smallest]) <= 0) break;

            Swap(i, smallest);
            i = smallest;
        }
    }

    private void Swap(int a, int b) =>
        (_buffer[a], _buffer[b]) = (_buffer[b], _buffer[a]);


    internal void Clear()
    {
        for (int i = 0; i < _count; i++)
            _buffer[i] = default;

        _count = 0;
    }
}