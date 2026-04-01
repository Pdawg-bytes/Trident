namespace Trident.Core.Hardware.Timers;

internal class TimerChannel
{
    internal int ID;

    internal ushort Reload;
    internal uint   Counter;

    internal int  Prescaler;
    internal bool Cascade;
    internal bool IRQEnabled;
    internal bool Enabled;

    internal bool  Running;
    internal ulong TimestampStarted;
}