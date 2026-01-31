using System.Numerics;
using Trident.Core.Bus;
using Trident.Core.Scheduling;
using Trident.Core.CPU.Pipeline;
using System.Runtime.CompilerServices;
using Trident.Core.Debugging.Snapshots;
using Trident.Core.Hardware.Interrupts;

namespace Trident.Core.Hardware.DMA;

internal partial class DMAManager
{
    private GBABusView _busView;
    private readonly Action<InterruptSource, int> _raiseIRQ;
    private readonly Scheduler _scheduler;

    private readonly DMAChannel[] _channels = new DMAChannel[4];
    private readonly DMASet _hblankDMA      = new();
    private readonly DMASet _vblankDMA      = new();
    private readonly DMASet _runnableDMA    = new();
    private bool _endVideoDMA = false;

    internal DMAManager(Action<InterruptSource, int> raiseIRQ, Scheduler scheduler)
    {
        _raiseIRQ = raiseIRQ;
        _scheduler = scheduler;

        _scheduler.Register(EventType.DMA_Activate, ActivateDMA);
        Reset();
    }

    internal void SetBusView(GBABusView busView) => _busView = busView;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Trigger(DMATrigger trigger) => Trigger(trigger, 0);
    internal void Trigger(DMATrigger trigger, uint scanline)
    {
        switch (trigger)
        {
            case DMATrigger.HBlank:
                if (scanline < 160)
                    Schedule(_hblankDMA.Raw);

                DMAChannel ch3 = _channels[3];
                if (ch3.Enabled && ch3.StartTiming == DMAStartTiming.Special)
                {
                    if (scanline < 162)
                    {
                        _endVideoDMA = scanline == 161;
                        Schedule(1u << 3);
                    }
                }
                break;
            case DMATrigger.VBlank:
                Schedule(_vblankDMA.Raw);
                break;
            case DMATrigger.FIFO0:
            case DMATrigger.FIFO1:
                {
                    int id = trigger == DMATrigger.FIFO0 ? 1 : 2;
                    DMAChannel ch = _channels[id];

                    if (ch.Enabled && ch.StartTiming == DMAStartTiming.Special)
                        Schedule(1u << id);
                }
                break;
        }
    }


    private void ActivateDMA(ulong id)
    {
        DMAChannel ch = _channels[id];
        
        if (!ch.Enabled)
            return;

        int unitSize       = ch.TransferSize == DMATransferSize.Word ? 4 : 2;
        uint transferCount = (ch.StartTiming == DMAStartTiming.Special && id >= 1 && id <= 2) ? 4 : ch.Latch.TransferLength;

        PipelineAccess access = PipelineAccess.NonSequential | PipelineAccess.DMA;

        for (uint i = 0; i < transferCount; i++)
        {
            if (ch.TransferSize == DMATransferSize.Word)
            {
                uint value = _busView.Read32(ch.Latch.Source, access);
                _busView.Write32(ch.Latch.Destination, value, access);
            }
            else
            {
                ushort value = _busView.Read16(ch.Latch.Source, access);
                _busView.Write16(ch.Latch.Destination, value, access);
            }

            ch.Latch.Source      = UpdateAddress(ch.Latch.Source, ch.SourceControl, unitSize);
            ch.Latch.Destination = UpdateAddress(ch.Latch.Destination, ch.DestinationControl, unitSize);

            access = PipelineAccess.Sequential | PipelineAccess.DMA;
        }

        bool isImmediate     = ch.StartTiming == DMAStartTiming.Immediate;
        bool isVideoTransfer = ch.StartTiming == DMAStartTiming.Special && id == 3;

        if (ch.Repeat && !isImmediate)
        {
            if (isVideoTransfer)
            {
                if (_endVideoDMA)
                {
                    uint transferMask    = (ch.TransferSize == DMATransferSize.Word) ? ~3u : ~1u;
                    ch.Latch.Destination = ch.Destination & transferMask;
                    _endVideoDMA         = false;
                }
            }
            else if (ch.DestinationControl == AddressingMode.Reload)
            {
                uint transferMask    = (ch.TransferSize == DMATransferSize.Word) ? ~3u : ~1u;
                ch.Latch.Destination = ch.Destination & transferMask;
            }
        }
        else
        {
            ch.Source      = ch.Latch.Source;
            ch.Destination = ch.Latch.Destination;
            ch.Enabled     = false;
            DequeueDMA(ch);
        }

        if (ch.InterruptOnEnd)
            _raiseIRQ(InterruptSource.DMA, (int)id);
    }

    private uint UpdateAddress(uint addr, AddressingMode mode, int step)
    {
        return mode switch
        {
            AddressingMode.Increment => addr + (uint)step,
            AddressingMode.Decrement => addr - (uint)step,
            AddressingMode.Fixed     => addr,
            _                        => addr
        };
    }


    private void InitializeDMA(uint id)
    {
        DMAChannel ch = _channels[id];

        uint transferMask    = (ch.TransferSize == DMATransferSize.Word) ? ~3u : ~1u;
        ch.Latch.Source      = ch.Source & transferMask;
        ch.Latch.Destination = ch.Destination & transferMask;

        uint lengthMask = (id == 3) ? 0xFFFFu : 0x3FFFu;
        ch.Latch.TransferLength = ch.TransferLength & lengthMask;

        if (ch.Latch.TransferLength == 0)
            ch.Latch.TransferLength = lengthMask + 1;

        if (ch.StartTiming == DMAStartTiming.Immediate)
            Schedule(1u << (int)id);
        else
            EnqueueDMA(ch);
    }


    private void Schedule(uint mask)
    {
        // TONC: "[...] it works as soon as you enable the DMA.
        //        Well actually it takes 2 cycles before it'll set in [...]"
        while (mask != 0)
        {
            uint id = (uint)BitOperations.TrailingZeroCount(mask);
            mask &= ~(1u << (int)id);

            _scheduler.Schedule(EventType.DMA_Activate, 2, ctx: id);
        }
    }

    private void EnqueueDMA(DMAChannel ch)
    {
        switch (ch.StartTiming)
        {
            case DMAStartTiming.VBlank:
                _vblankDMA.Set(ch.ID, true);
                break;
            case DMAStartTiming.HBlank:
                _hblankDMA.Set(ch.ID, true);
                break;
            case DMAStartTiming.Special:
                if (ch.ID == 3)
                    _runnableDMA.Set(ch.ID, true);
                break;
        }
    }

    private void DequeueDMA(DMAChannel ch)
    {
        _hblankDMA.Set(ch.ID, false);
        _vblankDMA.Set(ch.ID, false);
        _runnableDMA.Set(ch.ID, false);
    }


    internal void Reset()
    {
        for (uint i = 0; i < _channels.Length; i++)
            _channels[i] = new(i);

        _hblankDMA.Clear();
        _vblankDMA.Clear();
        _runnableDMA.Clear();
        _endVideoDMA = false;
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

    // All DMAs (bits 0-3) set
    internal readonly bool AllSet  => _bits == 0xF;
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