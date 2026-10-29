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

    private uint _hblankArmed;
    private uint _vblankArmed;

    private uint _activationQueued;

    private uint _ready;

    private bool _running;
    private bool _endVideoCapture;

    internal DMAManager(Action<InterruptSource, int> raiseIRQ, Scheduler scheduler)
    {
        _raiseIRQ  = raiseIRQ;
        _scheduler = scheduler;

        _scheduler.Register(EventType.DMA_Activate, OnActivate);
        Reset();
    }

    internal void SetBusView(GBABusView busView) => _busView = busView;


    internal void Trigger(DMATrigger trigger, uint scanline = 0)
    {
        switch (trigger)
        {
            case DMATrigger.HBlank:
                if (scanline < 160)
                    Enqueue(_hblankArmed);

                DMAChannel video = _channels[3];
                if (video.Enabled && video.StartTiming == DMAStartTiming.Special && scanline < 162)
                {
                    _endVideoCapture = scanline == 161;
                    Enqueue(1u << 3);
                }
                break;

            case DMATrigger.VBlank:
                Enqueue(_vblankArmed);
                break;

            case DMATrigger.FIFO0:
                QueueFIFO(1);
                break;

            case DMATrigger.FIFO1:
                QueueFIFO(2);
                break;
        }
    }

    private void QueueFIFO(int id)
    {
        DMAChannel ch = _channels[id];
        if (ch.Enabled && ch.StartTiming == DMAStartTiming.Special)
            Enqueue(1u << id);
    }


    // TONC: "[...] it works as soon as you enable the DMA.
    //        Well actually it takes 2 cycles before it'll set in [...]"
    private void Enqueue(uint mask)
    {
        mask &= ~_activationQueued;
        _activationQueued |= mask;

        while (mask != 0)
        {
            int id  = BitOperations.TrailingZeroCount(mask);
            mask   &= mask - 1;

            _scheduler.Schedule(EventType.DMA_Activate, 2, ctx: (ulong)id);
        }
    }

    private void OnActivate(ulong id)
    {
        uint bit = 1u << (int)id;
        _activationQueued &= ~bit;

        if (_channels[id].Enabled)
            _ready |= bit;

        if (_running)
            return;

        _running = true;
        while (_ready != 0)
        {
            int next  = BitOperations.TrailingZeroCount(_ready);
            _ready   &= ~(1u << next);

            RunChannel(_channels[next]);
        }
        _running = false;
    }

    private void RunChannel(DMAChannel ch)
    {
        if (!ch.Enabled)
            return;

        bool special = ch.StartTiming == DMAStartTiming.Special;
        bool isFIFO  = special && (ch.ID == 1 || ch.ID == 2);
        bool isVideo = special && ch.ID == 3;
        bool word    = isFIFO || ch.TransferSize == DMATransferSize.Word;

        AddressingMode dstMode = isFIFO ? AddressingMode.Fixed : ch.DestinationControl;

        uint unit    = word   ? 4u : 2u;
        uint count   = isFIFO ? 4u : ch.Latch.TransferLength;
        uint srcMask = DMASrcMask(ch.ID);
        uint dstMask = DMADstMask(ch.ID);

        uint src = ch.Latch.Source      & ~(unit - 1);
        uint dst = ch.Latch.Destination & ~(unit - 1);

        PipelineAccess access = PipelineAccess.NonSequential | PipelineAccess.DMA;

        for (uint i = 0; i < count; i++)
        {
            if (word)
                _busView.Write32(dst, _busView.Read32(src, access), access);
            else
                _busView.Write16(dst, _busView.Read16(src, access), access);

            src = StepSource(src, ch.SourceControl, unit) & srcMask;
            dst = StepDestination(dst, dstMode, unit)     & dstMask;

            access = PipelineAccess.Sequential | PipelineAccess.DMA;
        }

        ch.Latch.Source      = src;
        ch.Latch.Destination = dst;

        bool repeat = ch.Repeat && ch.StartTiming != DMAStartTiming.Immediate;

        if (isVideo && _endVideoCapture)
        {
            repeat = false;
            _endVideoCapture = false;
        }

        if (repeat)
        {
            if (!isFIFO)
                ch.Latch.TransferLength = GetUnitCount(ch);

            if (!isVideo && !isFIFO && ch.DestinationControl == AddressingMode.Reload)
                ch.Latch.Destination = ch.Destination & ~(unit - 1);
        }
        else
        {
            ch.Enabled = false;
            Dequeue(ch);
        }

        if (ch.InterruptOnEnd)
            _raiseIRQ(InterruptSource.DMA, (int)ch.ID);
    }

    private static uint StepSource(uint address, AddressingMode mode, uint step) => mode switch
    {
        AddressingMode.Increment => address + step,
        AddressingMode.Decrement => address - step,
        _                        => address
    };

    private static uint StepDestination(uint address, AddressingMode mode, uint step) => mode switch
    {
        AddressingMode.Increment or AddressingMode.Reload => address + step,
        AddressingMode.Decrement                          => address - step,
        _                                                 => address
    };

    private static uint GetUnitCount(DMAChannel ch)
    {
        uint max   = ch.ID == 3 ? 0x10000u : 0x4000u;
        uint count = ch.TransferLength & (max - 1);

        return count == 0 ? max : count;
    }


    private void InitializeDMA(DMAChannel ch)
    {
        uint align = ch.TransferSize == DMATransferSize.Word ? ~3u : ~1u;

        ch.Latch.Source         = ch.Source & align;
        ch.Latch.Destination    = ch.Destination & align;
        ch.Latch.TransferLength = GetUnitCount(ch);

        Dequeue(ch);

        uint bit = 1u << (int)ch.ID;
        switch (ch.StartTiming)
        {
            case DMAStartTiming.Immediate: Enqueue(bit);          break;
            case DMAStartTiming.VBlank:    _vblankArmed |= bit; break;
            case DMAStartTiming.HBlank:    _hblankArmed |= bit; break;
        }
    }

    private void Dequeue(DMAChannel ch)
    {
        uint bit      = ~(1u << (int)ch.ID);
        _hblankArmed &= bit;
        _vblankArmed &= bit;
        _ready       &= bit;
    }


    internal void Reset()
    {
        for (uint i = 0; i < _channels.Length; i++)
            _channels[i] = new(i);

        _hblankArmed      = 0;
        _vblankArmed      = 0;
        _activationQueued = 0;
        _ready            = 0;
        _running          = false;
        _endVideoCapture  = false;
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