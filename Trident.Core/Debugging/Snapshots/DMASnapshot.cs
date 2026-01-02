using Trident.Core.Hardware.DMA;

namespace Trident.Core.Debugging.Snapshots;

public readonly struct DMASnapshot(in DMASnapshot.ChannelSnapshot ch0, in DMASnapshot.ChannelSnapshot ch1, in DMASnapshot.ChannelSnapshot ch2, in DMASnapshot.ChannelSnapshot ch3)
{
    public readonly ChannelSnapshot Channel0 = ch0;
    public readonly ChannelSnapshot Channel1 = ch1;
    public readonly ChannelSnapshot Channel2 = ch2;
    public readonly ChannelSnapshot Channel3 = ch3;

    public readonly struct ChannelSnapshot
    (
        bool enabled,
        bool repeat,
        bool irq,
        bool drq,
        uint src,
        uint dst,
        ushort len,
        DMATransferSize size,
        AddressingMode srcCtrl,
        AddressingMode dstCtrl,
        DMAStartTiming startTiming
    )
    {
        public readonly bool Enabled        = enabled;
        public readonly bool Repeat         = repeat;
        public readonly bool InterruptOnEnd = irq;
        public readonly bool GamePakDRQ     = drq;

        public readonly uint Source           = src;
        public readonly uint Destination      = dst;
        public readonly ushort TransferLength = len;

        public readonly DMATransferSize TransferSize      = size;
        public readonly AddressingMode SourceControl      = srcCtrl;
        public readonly AddressingMode DestinationControl = dstCtrl;
        public readonly DMAStartTiming StartTiming        = startTiming;
    }
}