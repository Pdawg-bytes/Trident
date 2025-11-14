using Trident.Core.Bus;
using Trident.Core.Scheduling;
using Trident.Core.CPU.Pipeline;
using Trident.Core.Debugging.Snapshots;
using Trident.Core.Hardware.Interrupts;

namespace Trident.Core.Hardware.DMA
{
    internal partial class DMAManager
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