namespace Trident.Core.Debugging.Snapshots;

public readonly struct TimerSnapshot(in TimerSnapshot.ChannelSnapshot ch0, in TimerSnapshot.ChannelSnapshot ch1, in TimerSnapshot.ChannelSnapshot ch2, in TimerSnapshot.ChannelSnapshot ch3)
{
    public readonly ChannelSnapshot Channel0 = ch0;
    public readonly ChannelSnapshot Channel1 = ch1;
    public readonly ChannelSnapshot Channel2 = ch2;
    public readonly ChannelSnapshot Channel3 = ch3;

    public readonly struct ChannelSnapshot
    (
        bool enabled,
        ushort counter,
        ushort reload,
        int prescaler,
        bool cascade,
        bool irqEnabled,
        bool running)
    {
        public readonly bool Enabled    = enabled;
        public readonly ushort Counter  = counter;
        public readonly ushort Reload   = reload;
        public readonly int Prescaler   = prescaler;
        public readonly bool Cascade    = cascade;
        public readonly bool IRQEnabled = irqEnabled;
        public readonly bool Running    = running;
    }
}