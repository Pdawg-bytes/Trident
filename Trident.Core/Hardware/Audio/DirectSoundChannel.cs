namespace Trident.Core.Hardware.Audio;

internal sealed class DirectSoundChannel
{
    internal const int Capacity        = 32;
    internal const int RefillThreshold = 16;

    private readonly sbyte[] _queue = new sbyte[Capacity];
    private int _head;

    internal int   Count;
    internal sbyte Sample;

    internal bool Volume100;
    internal bool EnableRight;
    internal bool EnableLeft;
    internal int  TimerSelect;

    internal bool NeedsRefill => Count <= RefillThreshold;

    internal void Reset()
    {
        ClearQueue();
        Volume100   = false;
        EnableRight = false;
        EnableLeft  = false;
        TimerSelect = 0;
    }

    internal void ClearQueue()
    {
        _head  = 0;
        Count  = 0;
        Sample = 0;
    }

    internal void Push(sbyte value)
    {
        if (Count == Capacity)
            return;

        _queue[(_head + Count) & (Capacity - 1)] = value;
        Count++;
    }

    internal void Advance()
    {
        if (Count == 0)
            return;

        Sample = _queue[_head];
        _head  = (_head + 1) & (Capacity - 1);
        Count--;
    }
}