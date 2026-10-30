using Trident.Core.Memory.MappedIO;

namespace Trident.Core.Hardware.DMA;

internal partial class DMAManager
{
    internal ushort ReadDMAControl(uint id)
    {
        DMAChannel channel = _channels[id];
        return (ushort)
        (
            ((int)channel.DestinationControl      << 5) |
            ((int)channel.SourceControl           << 7) |
            (channel.Repeat             ? (1 << 9) : 0) |
            ((int)channel.TransferSize           << 10) |
            (channel.GamePakDRQ        ? (1 << 11) : 0) |
            ((int)channel.StartTiming            << 12) |
            (channel.InterruptOnEnd    ? (1 << 14) : 0) |
            (channel.Enabled           ? (1 << 15) : 0)
        );
    }

    internal void WriteDMAControlH(ushort value, WriteMask mask, uint id)
    {
        DMAChannel channel = _channels[id];

        if (mask.IsLower())
        {
            channel.DestinationControl = (AddressingMode)((value >> 5) & 0b11);
            channel.SourceControl      = (AddressingMode)(((int)channel.SourceControl & 0b10) | ((value >> 7) & 0b01));
        }

        if (mask.IsUpper())
        {
            bool wasEnabled = channel.Enabled;
            
            channel.SourceControl  = (AddressingMode)(((int)channel.SourceControl & 0b01) | (((value >> 8) & 0b01) << 1));
            channel.Repeat         = (value & (1 << 9)) != 0;
            channel.TransferSize   = (DMATransferSize)((value >> 10) & 1);
            channel.GamePakDRQ     = (value & (1 << 11)) != 0;
            channel.StartTiming    = (DMAStartTiming)((value >> 12) & 0b11);
            channel.InterruptOnEnd = (value & (1 << 14)) != 0;
            channel.Enabled        = (value & (1 << 15)) != 0;

            if (channel.Enabled && !wasEnabled)
                InitializeDMA(channel);
            else if (!channel.Enabled)
                Dequeue(channel);
        }
    }

    internal void WriteDMAControlL(ushort value, WriteMask mask, uint id)
    {
        DMAChannel channel     = _channels[id];
        channel.TransferLength = MMIO.Merge(channel.TransferLength, value, mask);
    }


    internal void WriteDMATarget(ushort value, WriteMask wordMask, WriteMask byteMask, uint id, bool source)
    {
        ref uint address = ref (source ? ref _channels[id].Source : ref _channels[id].Destination);

        address  = MMIO.Merge(address, value, wordMask, byteMask);
        address &= source ? DMASrcMask(id) : DMADstMask(id);
    }

    private static uint DMASrcMask(uint id) => id == 0 ? 0x07FFFFFFu : 0x0FFFFFFFu;
    private static uint DMADstMask(uint id) => id == 3 ? 0x0FFFFFFFu : 0x07FFFFFFu;
}