using System.Numerics;
using Trident.Core.Bus;
using Trident.Core.CPU;
using Trident.Core.Scheduling;
using Trident.Core.Debugging.Snapshots;
using Trident.Core.Hardware.Interrupts;

namespace Trident.Core.Hardware.DMA;

internal partial class DMAManager
{
    private GBABusView _busView;
    private readonly Action<InterruptSource, int> _raiseIRQ;
    private readonly Scheduler _scheduler;

    private readonly DMAChannel[] _channels = new DMAChannel[4];

    private readonly DMASet _hblankDMA = new();
    private readonly DMASet _vblankDMA = new();

    private uint _queuedMask;

    private uint _deferredMask;
    private bool _dmaRunning;

    private bool _endVideoDMA;

    internal DMAManager(Action<InterruptSource, int> raiseIRQ, Scheduler scheduler)
    {
        _raiseIRQ  = raiseIRQ;
        _scheduler = scheduler;

        _scheduler.Register(EventType.DMA_Activate, ActivateDMA);
        Reset();
    }

    internal void SetBusView(GBABusView busView) => _busView = busView;


    internal void Trigger(DMATrigger trigger, uint scanline = 0)
    {
        switch (trigger)
        {
            case DMATrigger.HBlank:
                if (scanline < 160)
                    Schedule(_hblankDMA.Raw);

                DMAChannel ch3 = _channels[3];
                if (ch3.Enabled && ch3.StartTiming == DMAStartTiming.Special && scanline < 162)
                {
                    _endVideoDMA = scanline == 161;
                    Schedule(1u << 3);
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
        _queuedMask &= ~(1u << (int)id);

        if (_dmaRunning)
        {
            _deferredMask |= 1u << (int)id;
            return;
        }

        _dmaRunning = true;
        RunTransfer(_channels[id]);

        while (_deferredMask != 0)
        {
            int next = BitOperations.TrailingZeroCount(_deferredMask);
            _deferredMask &= ~(1u << next);
            RunTransfer(_channels[next]);
        }

        _dmaRunning = false;
    }

    private void RunTransfer(DMAChannel ch)
    {
        if (!ch.Enabled)
            return;

        bool isFifo    = ch.StartTiming == DMAStartTiming.Special && ch.ID is 1 or 2;
        bool isVideo   = ch.StartTiming == DMAStartTiming.Special && ch.ID == 3;
        bool wordSized = isFifo || ch.TransferSize == DMATransferSize.Word;

        AddressingMode dstControl = isFifo ? AddressingMode.Fixed : ch.DestinationControl;

        uint unitSize      = wordSized ? 4u : 2u;
        uint transferCount = isFifo ? 4 : ch.Latch.TransferLength;
        uint srcMask       = DMASrcMask(ch.ID);
        uint dstMask       = DMADstMask(ch.ID);

        ch.Latch.Source      &= ~(unitSize - 1);
        ch.Latch.Destination &= ~(unitSize - 1);

        PipelineAccess access = PipelineAccess.NonSequential | PipelineAccess.DMA;

        for (uint i = 0; i < transferCount; i++)
        {
            if (wordSized)
            {
                uint value = _busView.Read32(ch.Latch.Source, access);
                _busView.Write32(ch.Latch.Destination, value, access);
            }
            else
            {
                ushort value = _busView.Read16(ch.Latch.Source, access);
                _busView.Write16(ch.Latch.Destination, value, access);
            }

            ch.Latch.Source      = StepAddress(ch.Latch.Source, ch.SourceControl, unitSize) & srcMask;
            ch.Latch.Destination = StepAddress(ch.Latch.Destination, dstControl, unitSize) & dstMask;

            access = PipelineAccess.Sequential | PipelineAccess.DMA;
        }

        bool keepRunning = ch.Repeat && ch.StartTiming != DMAStartTiming.Immediate;

        if (isVideo && _endVideoDMA)
        {
            keepRunning  = false;
            _endVideoDMA = false;
        }

        if (keepRunning)
        {
            ch.Latch.TransferLength = GetUnitCount(ch);

            if (!isVideo && !isFifo && ch.DestinationControl == AddressingMode.Reload)
                ch.Latch.Destination = ch.Destination & ~(unitSize - 1);
        }
        else
        {
            ch.Enabled = false;
            Dequeue(ch);
        }

        if (ch.InterruptOnEnd)
            _raiseIRQ(InterruptSource.DMA, (int)ch.ID);
    }

    private static uint StepAddress(uint address, AddressingMode mode, uint step) => mode switch
    {
        AddressingMode.Increment or AddressingMode.Reload => address + step,
        AddressingMode.Decrement                          => address - step,
        _                                                 => address
    };

    private static uint GetUnitCount(DMAChannel ch)
    {
        uint lengthMask = ch.ID == 3 ? 0xFFFFu : 0x3FFFu;
        uint count      = ch.TransferLength & lengthMask;
        return count == 0 ? lengthMask + 1 : count;
    }


    private void InitializeDMA(DMAChannel ch)
    {
        uint alignMask       = ch.TransferSize == DMATransferSize.Word ? ~3u : ~1u;
        ch.Latch.Source      = ch.Source & alignMask;
        ch.Latch.Destination = ch.Destination & alignMask;

        ch.Latch.TransferLength = GetUnitCount(ch);

        Dequeue(ch);

        switch (ch.StartTiming)
        {
            case DMAStartTiming.Immediate:
                Schedule(1u << (int)ch.ID);
                break;
            case DMAStartTiming.VBlank:
                _vblankDMA.Set(ch.ID, true);
                break;
            case DMAStartTiming.HBlank:
                _hblankDMA.Set(ch.ID, true);
                break;
        }
    }

    private void Dequeue(DMAChannel ch)
    {
        _hblankDMA.Set(ch.ID, false);
        _vblankDMA.Set(ch.ID, false);
    }

    private void Schedule(uint mask)
    {
        // TONC: "[...] it works as soon as you enable the DMA.
        //        Well actually it takes 2 cycles before it'll set in [...]"
        while (mask != 0)
        {
            int id = BitOperations.TrailingZeroCount(mask);
            mask &= ~(1u << id);

            if ((_queuedMask & (1u << id)) != 0)
                continue;

            _queuedMask |= 1u << id;
            _scheduler.Schedule(EventType.DMA_Activate, 2, ctx: (ulong)id);
        }
    }


    internal void Reset()
    {
        for (uint i = 0; i < _channels.Length; i++)
            _channels[i] = new(i);

        _hblankDMA.Clear();
        _vblankDMA.Clear();

        _queuedMask   = 0;
        _deferredMask = 0;
        _dmaRunning   = false;
        _endVideoDMA  = false;
    }


    internal DMASnapshot GetSnapshot()
    {
        return new
        (
            MakeChannelSnapshot(in _channels[0]),
            MakeChannelSnapshot(in _channels[1]),
            MakeChannelSnapshot(in _channels[2]),
            MakeChannelSnapshot(in _channels[3])
        );
    }

    private DMASnapshot.ChannelSnapshot MakeChannelSnapshot(in DMAChannel ch) => new
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


internal enum DMATrigger
{
    HBlank,
    VBlank,
    FIFO0,
    FIFO1
}


internal struct DMASet
{
    private byte _bits;
    internal readonly byte Raw => _bits;

    internal void Set(uint index, bool value)
    {
        if (value)
            _bits |= (byte)(1 << (int)index);
        else
            _bits &= (byte)~(1 << (int)index);
    }

    internal void Clear() => _bits = 0;
}