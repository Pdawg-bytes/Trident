using Trident.Core.Scheduling;
using Trident.Core.Memory.MappedIO;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Core.Hardware.Timers;

internal class TimerManager
{
    private readonly int[] _prescalerShifts = [0, 6, 8, 10];

    private readonly TimerChannel[] _channels = new TimerChannel[4];
    private readonly Action<InterruptSource, int> _raiseIRQ;
    private readonly Scheduler _scheduler;

    internal TimerManager(Action<InterruptSource, int> raiseIRQ, Scheduler scheduler)
    {
        _raiseIRQ  = raiseIRQ;
        _scheduler = scheduler;

        for (int i = 0; i < 4; i++)
            _channels[i] = new TimerChannel { ID = i };

        _scheduler.Register(EventType.TMR_Overflow, OnOverflow);

        Reset();
    }


    internal void Reset()
    {
        for (int i = 0; i < 4; i++)
        {
            TimerChannel ch     = _channels[i];
            ch.Reload           = 0;
            ch.Counter          = 0;
            ch.Prescaler        = 0;
            ch.Cascade          = false;
            ch.IRQEnabled       = false;
            ch.Enabled          = false;
            ch.Running          = false;
            ch.TimestampStarted = 0;
        }
    }


    internal ushort ReadCounterReload(int id)
    {
        TimerChannel ch = _channels[id];

        if (ch.Running)
            return (ushort)(ch.Counter + GetCounterDelta(ch));

        return (ushort)ch.Counter;
    }

    internal void WriteReload(int id, ushort value, WriteMask mask)
    {
        TimerChannel ch = _channels[id];

        if (mask.IsLower())
            ch.Reload = (ushort)((ch.Reload & 0xFF00) | (value & 0x00FF));

        if (mask.IsUpper())
            ch.Reload = (ushort)((ch.Reload & 0x00FF) | (value & 0xFF00));
    }

    internal ushort ReadControl(int id)
    {
        TimerChannel ch = _channels[id];

        return (ushort)(
            (ch.Prescaler & 3) |
            (ch.Cascade    ? (1 << 2) : 0) |
            (ch.IRQEnabled ? (1 << 6) : 0) |
            (ch.Enabled    ? (1 << 7) : 0)
        );
    }

    internal void WriteControl(int id, ushort value, WriteMask mask)
    {
        if (!mask.IsLower()) return;

        TimerChannel ch = _channels[id];
        bool wasEnabled = ch.Enabled;

        if (ch.Running)
            StopChannel(ch);

        ch.Prescaler  = value & 3;
        ch.IRQEnabled = (value & (1 << 6)) != 0;
        ch.Enabled    = (value & (1 << 7)) != 0;

        if (id != 0)
            ch.Cascade = (value & (1 << 2)) != 0;

        if (ch.Enabled)
        {
            if (!wasEnabled)
                ch.Counter = ch.Reload;

            if (!ch.Cascade)
                StartChannel(ch);
        }
    }


    private uint GetCounterDelta(TimerChannel ch)
    {
        int shift = _prescalerShifts[ch.Prescaler];
        return (uint)((_scheduler.CurrentTimestamp - ch.TimestampStarted) >> shift);
    }

    private void StartChannel(TimerChannel ch)
    {
        int shift = _prescalerShifts[ch.Prescaler];
        ulong cyclesToOverflow = (ulong)(0x10000 - ch.Counter) << shift;

        ch.Running = true;
        ch.TimestampStarted = _scheduler.CurrentTimestamp;

        _scheduler.Schedule(EventType.TMR_Overflow, cyclesToOverflow, ctx: (ulong)ch.ID);
    }

    private void StopChannel(TimerChannel ch)
    {
        if (!ch.Running) return;

        ch.Counter += GetCounterDelta(ch);

        if (ch.Counter >= 0x10000)
            HandleOverflowCascade(ch);

        ch.Running = false;
    }

    private void OnOverflow(ulong channelId)
    {
        TimerChannel ch = _channels[(int)channelId];

        if (!ch.Enabled || !ch.Running)
            return;

        HandleOverflowCascade(ch);
        StartChannel(ch);
    }

    private void HandleOverflowCascade(TimerChannel ch)
    {
        ch.Counter = ch.Reload;

        if (ch.IRQEnabled)
            _raiseIRQ(InterruptSource.Timer, ch.ID);

        if (ch.ID < 3)
        {
            TimerChannel next = _channels[ch.ID + 1];
            if (next.Enabled && next.Cascade)
            {
                next.Counter++;
                if (next.Counter >= 0x10000)
                    HandleOverflowCascade(next);
            }
        }
    }


    internal TimerSnapshot GetSnapshot()
    {
        return new TimerSnapshot
        (
            MakeChannelSnapshot(0),
            MakeChannelSnapshot(1),
            MakeChannelSnapshot(2),
            MakeChannelSnapshot(3)
        );
    }

    private TimerSnapshot.ChannelSnapshot MakeChannelSnapshot(int id)
    {
        TimerChannel ch = _channels[id];
        ushort currentCounter = ch.Running
            ? (ushort)(ch.Counter + GetCounterDelta(ch))
            : (ushort)ch.Counter;

        return new TimerSnapshot.ChannelSnapshot
        (
            enabled:    ch.Enabled,
            counter:    currentCounter,
            reload:     ch.Reload,
            prescaler:  ch.Prescaler,
            cascade:    ch.Cascade,
            irqEnabled: ch.IRQEnabled,
            running:    ch.Running
        );
    }
}