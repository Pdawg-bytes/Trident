using Trident.Core.Memory.MappedIO;

namespace Trident.Core.Hardware.Audio.PSG;

internal sealed class SquareChannel(bool hasSweep) : PSGChannel
{
    private static readonly byte[] DutyPatterns = [ 0b00000001, 0b10000001, 0b10000111, 0b01111110 ];
    private static readonly int[] DutyHighSteps = [ 1, 2, 4, 6 ];

    private readonly Envelope _envelope = new();

    private ushort _sweepRegister;
    private ushort _controlRegister;
    private ushort _frequencyRegister;

    private int _frequency;
    private int _timer;
    private int _step;

    private int _shadowFrequency;
    private int _sweepTimer;
    private bool _sweepEnabled;

    protected override int MaxLength => 64;

    private int SweepShift     => _sweepRegister & 7;
    private bool SweepDecrease => (_sweepRegister & (1 << 3)) != 0;
    private int SweepPeriod    => (_sweepRegister >> 4) & 7;

    internal override int Output
    {
        get
        {
            if (!Active)
                return 0;

            int duty   = (_controlRegister >> 6) & 3;
            int high   = DutyHighSteps[duty];
            int volume = _envelope.Volume;
            bool on    = ((DutyPatterns[duty] >> _step) & 1) != 0;

            return on ? volume * (8 - high) : -volume * high;
        }
    }

    internal override void Reset()
    {
        base.Reset();
        _envelope.Reset();

        _sweepRegister     = 0;
        _controlRegister   = 0;
        _frequencyRegister = 0;
        _frequency         = 0;
        _timer             = 0;
        _step              = 0;
        _sweepEnabled      = false;
    }



    internal ushort ReadSweep() => _sweepRegister;

    internal void WriteSweep(ushort value, WriteMask mask)
    {
        if (!hasSweep)
            return;

        _sweepRegister = (ushort)(MMIO.Merge(_sweepRegister, value, mask) & 0x7F);
    }


    internal ushort ReadControl() => (ushort)(_controlRegister & 0xFFC0);

    internal void WriteControl(ushort value, WriteMask mask)
    {
        _controlRegister = MMIO.Merge(_controlRegister, value, mask);

        if (mask.IsLower())
            LoadLength(value & 0x3F);

        _envelope.Configure(_controlRegister);

        if (!_envelope.DACEnabled)
            Active = false;
    }


    internal ushort ReadFrequency() => (ushort)(_frequencyRegister & 0x4000);

    internal void WriteFrequency(ushort value, WriteMask mask)
    {
        _frequencyRegister = (ushort)(MMIO.Merge(_frequencyRegister, value, mask) & 0x47FF);
        _frequency = _frequencyRegister & 0x7FF;
        LengthEnabled = (_frequencyRegister & 0x4000) != 0;

        if (mask.IsUpper() && (value & 0x8000) != 0)
            Restart();
    }



    private void Restart()
    {
        Active = _envelope.DACEnabled;

        ReloadEmptyLength();
        _envelope.Restart();

        _timer = GetPeriod();
        _step  = 0;

        if (!hasSweep)
            return;

        _shadowFrequency = _frequency;
        _sweepTimer      = SweepPeriod == 0 ? 8 : SweepPeriod;
        _sweepEnabled    = SweepPeriod != 0 || SweepShift != 0;

        if (SweepShift != 0)
            CalculateSweep();
    }


    private int GetPeriod() => (2048 - _frequency) * 16;

    internal override void Tick(int cycles)
    {
        if (!Active)
            return;

        _timer -= cycles;

        while (_timer <= 0)
        {
            _timer += GetPeriod();
            _step   = (_step + 1) & 7;
        }
    }

    internal override void ClockEnvelope() => _envelope.Clock();


    internal void ClockSweep()
    {
        if (!hasSweep || !_sweepEnabled)
            return;

        if (--_sweepTimer > 0)
            return;

        _sweepTimer = SweepPeriod == 0 ? 8 : SweepPeriod;

        if (SweepPeriod == 0)
            return;

        int next = CalculateSweep();
        if (next > 2047 || SweepShift == 0)
            return;

        _shadowFrequency = next;
        _frequency = next;
        CalculateSweep();
    }

    private int CalculateSweep()
    {
        int delta = _shadowFrequency >> SweepShift;
        int next  = SweepDecrease ? _shadowFrequency - delta : _shadowFrequency + delta;

        if (next > 2047)
            Active = false;

        return next;
    }
}