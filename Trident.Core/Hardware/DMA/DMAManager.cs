using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;
using Trident.Core.Debugging.Snapshots;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Memory.MappedIO;
using Trident.Core.Scheduling;

namespace Trident.Core.Hardware.DMA
{
    internal class DMAManager
    {
        private GBABusView _busView;
        private readonly Action<InterruptSource, int> _raiseIRQ;
        private readonly Scheduler _scheduler;

        private readonly DMAChannel[] _channels = new DMAChannel[4];

        internal DMAManager(Action<InterruptSource, int> raiseIRQ, Scheduler scheduler)
        {
            _raiseIRQ = raiseIRQ;
            _scheduler = scheduler;

            Reset();
        }

        internal void SetBusView(GBABusView busView) => _busView = busView;


        // worlds worst DMA award
        private void RunDMA(uint id)
        {
            DMAChannel ch = _channels[id];

            uint transferAlignment = (ch.TransferSize == DMATransferSize.Word ? ~3u : ~1u);
            ch.Latch.Source        = (ch.Source      & transferAlignment) & (id >  0 ? 0x0FFFFFFFu : 0x07FFFFFFu);
            ch.Latch.Destination   = (ch.Destination & transferAlignment) & (id == 3 ? 0x0FFFFFFFu : 0x07FFFFFFu);

            ch.TransferLength &= (id == 3) ? (ushort)0xFFFF : (ushort)0x3FFF;

            if (ch.StartTiming == DMAStartTiming.Immediate)
            {
                if (id != 3) return;

                int unitSize = ch.TransferSize == DMATransferSize.Word ? 4 : 2;

                for (int i = 0; i < ch.TransferLength; i++)
                {
                    uint value = ch.TransferSize == DMATransferSize.Word
                        ? _busView.Read32(ch.Latch.Source, PipelineAccess.NonSequential | PipelineAccess.DMA)
                        : _busView.Read16(ch.Latch.Source, PipelineAccess.NonSequential | PipelineAccess.DMA);

                    if (ch.TransferSize == DMATransferSize.Word)
                        _busView.Write32(ch.Latch.Destination, value, PipelineAccess.NonSequential | PipelineAccess.DMA);
                    else
                        _busView.Write16(ch.Latch.Destination, (ushort)value, PipelineAccess.NonSequential | PipelineAccess.DMA);

                    ch.Latch.Source      = UpdateAddress(ch.Latch.Source, ch.SourceControl, unitSize);
                    ch.Latch.Destination = UpdateAddress(ch.Latch.Destination, ch.DestinationControl, unitSize);
                }

                ch.Source = ch.Latch.Source;
                ch.Destination = ch.Latch.Destination;

                ch.Enabled = false;
            }
        }

        private static uint UpdateAddress(uint addr, AddressingMode mode, int step)
        {
            return mode switch
            {
                AddressingMode.Increment => addr + (uint)step,
                AddressingMode.Decrement => addr - (uint)step,
                AddressingMode.Fixed => addr,
                _ => addr
            };
        }


        internal ushort ReadDMAControl(uint id)
        {
            DMAChannel channel = _channels[id];
            return (ushort)
            (
                ((int)channel.DestinationControl  <<  5) | 
                ((int)channel.SourceControl       <<  7) |
                (channel.Repeat ?         (1 << 9)  : 0) |
                ((int)channel.TransferSize        << 10) |
                (channel.GamePakDRQ ?     (1 << 11) : 0) |
                ((int)channel.StartTiming         << 12) |
                (channel.InterruptOnEnd ? (1 << 14) : 0) |
                (channel.Enabled ?        (1 << 15) : 0)
            );
        }

        internal void WriteDMAControlH(ushort value, WriteMask mask, uint id)
        {
            DMAChannel channel = _channels[id];

            if (mask.IsLower())
            {
                channel.DestinationControl = (AddressingMode)((value >> 5) & 0b11);
                channel.SourceControl      = (AddressingMode)(((int)channel.SourceControl & 0b10) | (value >> 7));
            }

            if (mask.IsUpper())
            {
                channel.SourceControl  = (AddressingMode)(((int)channel.SourceControl & 0b01) | ((value & 0b01) << 1));
                channel.Repeat         = (value & (1 << 9))  != 0;
                channel.TransferSize   = (DMATransferSize)((value >> 10) & 1);
                channel.GamePakDRQ     = (value & (1 << 11)) != 0;
                channel.StartTiming    = (DMAStartTiming)((value >> 12) & 0b11);
                channel.InterruptOnEnd = (value & (1 << 14)) != 0;
                channel.Enabled        = (value & (1 << 15)) != 0;

                if (channel.Enabled && channel.StartTiming == DMAStartTiming.Immediate)
                    RunDMA(id);
            }
        }

        internal void WriteDMAControlL(ushort value, WriteMask mask, uint id)
        {
            DMAChannel channel = _channels[id];

            if (mask.IsLower())
                channel.TransferLength = (ushort)((channel.TransferLength & 0xFF00) | value & 0x00FF);

            if (mask.IsUpper())
                channel.TransferLength = (ushort)((channel.TransferLength & 0x00FF) | value & 0xFF00);
        }


        internal void WriteDMATarget(ushort value, WriteMask wordMask, WriteMask byteMask, uint id, bool source)
        {
            ref uint address = ref (source ? ref _channels[id].Source : ref _channels[id].Destination);

            uint lo = (uint)(value & 0x00FF);
            uint hi = (uint)(value & 0xFF00);

            switch (wordMask)
            {
                case WriteMask.Lower:
                    if (byteMask.IsLower())
                        address = (address & ~0x000000FFu) | lo;
                    if (byteMask.IsUpper())
                        address = (address & ~0x0000FF00u) | hi;
                    break;

                case WriteMask.Upper:
                    if (byteMask.IsLower())
                        address = (address & ~0x00FF0000u) | (lo << 16);
                    if (byteMask.IsUpper())
                        address = (address & ~0xFF000000u) | (hi << 16);
                    break;
            }
        }


        internal void Reset()
        {
            for (uint i = 0; i < _channels.Length; i++)
                _channels[i] = new(i);
        }


        internal DMASnapshot GetSnapshot()
        {
            ref readonly DMAChannel ch0 = ref _channels[0];
            ref readonly DMAChannel ch1 = ref _channels[1];
            ref readonly DMAChannel ch2 = ref _channels[2];
            ref readonly DMAChannel ch3 = ref _channels[3];

            return new
            (
                MakeChannelSnapshot(in ch0),
                MakeChannelSnapshot(in ch1),
                MakeChannelSnapshot(in ch2),
                MakeChannelSnapshot(in ch3)
            );
        }

        private DMASnapshot.ChannelSnapshot MakeChannelSnapshot(in DMAChannel ch) => new DMASnapshot.ChannelSnapshot
        (
            ch.Enabled,
            ch.Repeat,
            ch.InterruptOnEnd,
            ch.GamePakDRQ,
            ch.Source,
            ch.Destination,
            ch.TransferLength,
            ch.TransferSize,
            ch.SourceControl,
            ch.DestinationControl,
            ch.StartTiming
        );
    }
}