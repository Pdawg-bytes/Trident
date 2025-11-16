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
        private readonly DMASet _hblankDMA = new();
        private readonly DMASet _vblankDMA = new();

        internal DMAManager(Action<InterruptSource, int> raiseIRQ, Scheduler scheduler)
        {
            _raiseIRQ = raiseIRQ;
            _scheduler = scheduler;

            _scheduler.Register(EventType.DMA_Activate, ActivateDMA);
            Reset();
        }

        internal void SetBusView(GBABusView busView) => _busView = busView;


        private void ActivateDMA(ulong id)
        {
            DMAChannel ch = _channels[id];

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

                    ch.Latch.Source = UpdateAddress(ch.Latch.Source, ch.SourceControl, unitSize);
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


        private void InitializeDMA(uint id)
        {
            DMAChannel ch = _channels[id];

            uint transferMask    = (ch.TransferSize == DMATransferSize.Word) ? ~3u : ~1u;
            ch.Latch.Source      = ch.Source & transferMask;
            ch.Latch.Destination = ch.Destination & transferMask;

            uint lengthMask         = (id == 3) ? (ushort)0xFFFF : (ushort)0x3FFF;
            ch.Latch.TransferLength = (ushort)(ch.TransferLength & lengthMask);

            if (ch.Latch.TransferLength == 0)
                ch.Latch.TransferLength = (ushort)(lengthMask + 1);

            if (ch.StartTiming == DMAStartTiming.Immediate)
                ScheduleNextDMA(id);
            else
                EnqueueDMA(ch);
        }

        private void ScheduleNextDMA(uint id)
        {
            // TONC: "[...] it works as soon as you enable the DMA.
            //        Well actually it takes 2 cycles before it'll set in [...]"
            
            // TODO: later handle priority
            _scheduler.Schedule(EventType.DMA_Activate, 2, ctx: id);
        }

        private void EnqueueDMA(DMAChannel ch)
        {
            // TODO: handle "special" timing
            switch (ch.StartTiming)
            {
                case DMAStartTiming.VBlank:
                    _vblankDMA.Set(ch.ID, true);
                    break;
                case DMAStartTiming.HBlank:
                    _hblankDMA.Set(ch.ID, true);
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


    internal struct DMASet
    {
        private byte _bits;
        internal readonly byte Raw => _bits;

        internal readonly bool AllSet  => _bits == byte.MaxValue;
        internal readonly bool NoneSet => _bits == 0;

        internal void Set(uint index, bool value)
        {
            if (value)
                _bits |= (byte)(1 << (int)index);
            else
                _bits &= (byte)~(1 << (int)index);
        }

        internal bool Get(int index) => (_bits & (1u << index)) != 0;

        internal void Clear() => _bits = 0;
    }
}