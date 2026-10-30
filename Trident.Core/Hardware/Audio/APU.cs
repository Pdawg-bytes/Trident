using Trident.Core.Scheduling;
using Trident.Core.Hardware.Audio.PSG;
using Trident.Core.Hardware.DMA;
using Trident.Core.Memory.MappedIO;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Core.Hardware.Audio;

internal class APU
{
    private const int BaseSamplePeriod = 512;
    private const int SequencerPeriod  = 32768;
    private const int PSGSubStep       = 32;

    internal const int ScopeSources = 8;
    internal const int ScopeLength  = Machine.GBA.AudioScopeLength;

    private readonly Scheduler _scheduler;
    private readonly Action<DMATrigger, uint> _triggerDMA;
    private readonly AudioStream _stream;

    private readonly DirectSoundChannel[] _fifo = [new(), new()];

    internal readonly SquareChannel Square1 = new(hasSweep: true);
    internal readonly SquareChannel Square2 = new(hasSweep: false);
    internal readonly WaveChannel   Wave    = new();
    internal readonly NoiseChannel  Noise   = new();
    private readonly PSGChannel[] _psg;

    private const float DCBlockRate = 0.995f;
    private const float PSGGain     = 0.7f;

    private float _prevPsgLeft;
    private float _prevPsgRight;
    private float _filteredPsgLeft;
    private float _filteredPsgRight;

    private readonly short[][] _scope = Enumerable.Range(0, ScopeSources).Select(_ => new short[ScopeLength]).ToArray();
    private int _scopePosition;

    private int _sequencerCycles;
    private int _sequencerStep;

    private ushort _soundControlL;
    private int    _soundControlHPsgVolume;
    private ushort _soundBias;
    private bool   _masterEnable;

    private volatile bool _muted;

    internal APU(Scheduler scheduler, Action<DMATrigger, uint> triggerDMA, AudioStream stream)
    {
        _scheduler  = scheduler;
        _triggerDMA = triggerDMA;
        _stream     = stream;
        _psg        = [ Square1, Square2, Wave, Noise ];

        _scheduler.Register(EventType.APU_Sample, OnSample);

        Reset();
    }

    internal bool Muted
    {
        get => _muted;
        set
        {
            _muted = value;
            _stream.RequestClear();
        }
    }


    internal void Reset()
    {
        _fifo[0].Reset();
        _fifo[1].Reset();

        ResetPSG();

        _soundControlHPsgVolume = 0;
        _soundBias              = 0x0200;
        _masterEnable           = false;

        foreach (short[] ring in _scope)
            Array.Clear(ring);

        _stream.RequestClear();

        _scheduler.Schedule(EventType.APU_Sample, GetSamplePeriod());
    }

    private void ResetPSG()
    {
        foreach (PSGChannel channel in _psg)
            channel.Reset();

        _soundControlL   = 0;
        _prevPsgLeft     = _prevPsgRight     = 0;
        _filteredPsgLeft = _filteredPsgRight = 0;
        _sequencerCycles = 0;
        _sequencerStep   = 0;
    }


    private ulong GetSamplePeriod() => (ulong)(BaseSamplePeriod >> ((_soundBias >> 14) & 3));

    private void OnSample()
    {
        ulong period = GetSamplePeriod();
        _scheduler.Schedule(EventType.APU_Sample, period);

        if (_muted)
        {
            if (_masterEnable)
                AdvancePSG((int)period);

            return;
        }

        int bias  = _soundBias & 0x3FE;
        int left  = bias;
        int right = bias;

        if (_masterEnable)
        {
            (float psgLeft, float psgRight) = RunPSG((int)period);

            _filteredPsgLeft  = psgLeft  - _prevPsgLeft  + DCBlockRate * _filteredPsgLeft;
            _filteredPsgRight = psgRight - _prevPsgRight + DCBlockRate * _filteredPsgRight;
            _prevPsgLeft      = psgLeft;
            _prevPsgRight     = psgRight;

            left  += (int)(_filteredPsgLeft  * PSGGain);
            right += (int)(_filteredPsgRight * PSGGain);

            for (int i = 0; i < 2; i++)
            {
                DirectSoundChannel ch = _fifo[i];
                int value = ch.Sample << (ch.Volume100 ? 2 : 1);

                if (ch.EnableLeft)  left  += value;
                if (ch.EnableRight) right += value;
            }
        }

        left  = Math.Clamp(left,  0, 0x3FF);
        right = Math.Clamp(right, 0, 0x3FF);

        RecordScope(left - 0x200, right - 0x200);

        _stream.Write((short)(left - 0x200), (short)(right - 0x200), (_soundBias >> 14) & 3);
    }


    private (float Left, float Right) RunPSG(int cycles)
    {
        int steps      = Math.Max(1, cycles / PSGSubStep);
        int stepCycles = cycles / steps;

        int left  = 0;
        int right = 0;

        for (int i = 0; i < steps; i++)
        {
            AdvancePSG(stepCycles);

            (int stepLeft, int stepRight) = MixPSG();
            left  += stepLeft;
            right += stepRight;
        }

        return ((float)left / steps, (float)right / steps);
    }

    private (int Left, int Right) MixPSG()
    {
        int left  = 0;
        int right = 0;

        for (int i = 0; i < _psg.Length; i++)
        {
            int level = _psg[i].Output * 2 - 15;

            if ((_soundControlL & (1 << (12 + i))) != 0) left  += level;
            if ((_soundControlL & (1 << (8  + i))) != 0) right += level;
        }

        left  *= ((_soundControlL >> 4) & 7) + 1;
        right *= (_soundControlL        & 7) + 1;

        int shift = (_soundControlHPsgVolume == 0 ? 2 : _soundControlHPsgVolume == 1 ? 1 : 0) + 2;
        return (left >> shift, right >> shift);
    }

    private void AdvancePSG(int cycles)
    {
        foreach (PSGChannel channel in _psg)
            channel.Tick(cycles);

        _sequencerCycles += cycles;
        while (_sequencerCycles >= SequencerPeriod)
        {
            _sequencerCycles -= SequencerPeriod;
            ClockSequencer();
        }
    }

    private void ClockSequencer()
    {
        if ((_sequencerStep & 1) == 0)
        {
            foreach (PSGChannel channel in _psg)
                channel.ClockLength();
        }

        if ((_sequencerStep & 3) == 2)
            Square1.ClockSweep();

        if (_sequencerStep == 7)
        {
            Square1.ClockEnvelope();
            Square2.ClockEnvelope();
            Noise.ClockEnvelope();
        }

        _sequencerStep = (_sequencerStep + 1) & 7;
    }


    private void RecordScope(int left, int right)
    {
        int p = _scopePosition;

        _scope[0][p] = _masterEnable ? _fifo[0].Sample : (short)0;
        _scope[1][p] = _masterEnable ? _fifo[1].Sample : (short)0;

        for (int i = 0; i < _psg.Length; i++)
            _scope[2 + i][p] = (short)(_psg[i].Output >> 2);

        _scope[6][p] = (short)left;
        _scope[7][p] = (short)right;

        _scopePosition = (p + 1) & (ScopeLength - 1);
    }

    internal void CopyScope(int source, Span<short> destination)
    {
        short[] ring = _scope[source];
        int start    = _scopePosition;
        int count    = Math.Min(destination.Length, ScopeLength);

        for (int i = 0; i < count; i++)
            destination[i] = ring[(start + i) & (ScopeLength - 1)];
    }


    internal APUSnapshot GetSnapshot()
    {
        return new APUSnapshot
        (
            _masterEnable,
            32768 << ((_soundBias >> 14) & 3),
            _soundBias & 0x3FE,
            _soundControlHPsgVolume,
            (_soundControlL >> 4) & 7,
            _soundControlL & 7,
            MakeFIFOSnapshot(_fifo[0]),
            MakeFIFOSnapshot(_fifo[1]),
            MakePSGSnapshot(0),
            MakePSGSnapshot(1),
            MakePSGSnapshot(2),
            MakePSGSnapshot(3)
        );
    }

    private static APUSnapshot.FIFOSnapshot MakeFIFOSnapshot(DirectSoundChannel ch) => new
    (
        ch.EnableLeft,
        ch.EnableRight,
        ch.TimerSelect,
        ch.Volume100,
        ch.Count,
        ch.Sample
    );

    private APUSnapshot.PSGSnapshot MakePSGSnapshot(int id)
    {
        PSGChannel ch = _psg[id];

        return new APUSnapshot.PSGSnapshot
        (
            ch.Active,
            (_soundControlL & (1 << (12 + id))) != 0,
            (_soundControlL & (1 << (8 + id)))  != 0,
            ch.LengthActive,
            ch.Output
        );
    }


    internal void OnTimerOverflow(int timerId)
    {
        if (!_masterEnable)
            return;

        for (int i = 0; i < 2; i++)
        {
            DirectSoundChannel channel = _fifo[i];
            if (channel.TimerSelect != timerId)
                continue;

            channel.Advance();

            if (channel.NeedsRefill)
                _triggerDMA(i == 0 ? DMATrigger.FIFO0 : DMATrigger.FIFO1, 0);
        }
    }



    internal ushort ReadControlL() => _soundControlL;

    internal void WriteControlL(ushort value, WriteMask mask)
    {
        if (!_masterEnable)
            return;

        ushort merged  = MMIO.Merge(_soundControlL, value, mask);
        _soundControlL = (ushort)(merged & 0xFF77);
    }


    internal ushort ReadControlH()
    {
        DirectSoundChannel a = _fifo[0];
        DirectSoundChannel b = _fifo[1];

        return (ushort)
        (
            (_soundControlHPsgVolume & 3)  |
            (a.Volume100    ? 1 << 2 : 0)  |
            (b.Volume100    ? 1 << 3 : 0)  |
            (a.EnableRight  ? 1 << 8 : 0)  |
            (a.EnableLeft   ? 1 << 9 : 0)  |
            (a.TimerSelect      << 10)     |
            (b.EnableRight  ? 1 << 12 : 0) |
            (b.EnableLeft   ? 1 << 13 : 0) |
            (b.TimerSelect      << 14)
        );
    }

    internal void WriteControlH(ushort value, WriteMask mask)
    {
        DirectSoundChannel a = _fifo[0];
        DirectSoundChannel b = _fifo[1];

        if (mask.IsLower())
        {
            _soundControlHPsgVolume = value & 3;
            a.Volume100             = (value & (1 << 2)) != 0;
            b.Volume100             = (value & (1 << 3)) != 0;
        }

        if (mask.IsUpper())
        {
            a.EnableRight = (value & (1 << 8))  != 0;
            a.EnableLeft  = (value & (1 << 9))  != 0;
            a.TimerSelect = (value >> 10) & 1;

            b.EnableRight = (value & (1 << 12)) != 0;
            b.EnableLeft  = (value & (1 << 13)) != 0;
            b.TimerSelect = (value >> 14) & 1;

            if ((value & (1 << 11)) != 0) a.ClearQueue();
            if ((value & (1 << 15)) != 0) b.ClearQueue();
        }
    }


    internal ushort ReadControlX()
    {
        int flags = _masterEnable ? 1 << 7 : 0;

        for (int i = 0; i < _psg.Length; i++)
        {
            if (_psg[i].Active)
                flags |= 1 << i;
        }

        return (ushort)flags;
    }

    internal void WriteControlX(ushort value, WriteMask mask)
    {
        if (!mask.IsLower())
            return;

        bool enable = (value & (1 << 7)) != 0;

        if (_masterEnable && !enable)
            ResetPSG();

        _masterEnable = enable;
    }


    internal ushort ReadBias() => _soundBias;

    internal void WriteBias(ushort value, WriteMask mask)
    {
        ushort merged = MMIO.Merge(_soundBias, value, mask);
        _soundBias    = (ushort)(merged & 0xC3FE);
    }


    internal void WriteFIFO(int id, ushort value, WriteMask mask)
    {
        DirectSoundChannel channel = _fifo[id];

        if (mask.IsLower())
            channel.Push((sbyte)value);

        if (mask.IsUpper())
            channel.Push((sbyte)(value >> 8));
    }
}