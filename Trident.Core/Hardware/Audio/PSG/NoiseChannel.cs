using Trident.Core.Memory.MappedIO;

namespace Trident.Core.Hardware.Audio.PSG;

internal sealed class NoiseChannel : PSGChannel
{
    private readonly Envelope _envelope = new();

    private ushort _controlRegister;
    private ushort _frequencyRegister;

    private int  _timer;
    private int  _lfsr;
    private bool _high;

    private bool NarrowWidth         => (_frequencyRegister & (1 << 3)) != 0;
    protected override int MaxLength => 64;
    internal override int Output     => !Active ? 0 : _high ? 4 * _envelope.Volume : -4 * _envelope.Volume;

    internal override void Reset()
    {
        base.Reset();
        _envelope.Reset();

        _controlRegister   = 0;
        _frequencyRegister = 0;
        _timer             = 0;
        _lfsr              = 0x4000;
        _high              = false;
    }


    internal ushort ReadControl() => (ushort)(_controlRegister & 0xFF00);

    internal void WriteControl(ushort value, WriteMask mask)
    {
        _controlRegister = (ushort)(MMIO.Merge(_controlRegister, value, mask) & 0xFF3F);

        if (mask.IsLower())
            LoadLength(value & 0x3F);

        _envelope.Configure(_controlRegister);

        if (!_envelope.DACEnabled)
            Active = false;
    }

    internal ushort ReadFrequency() => (ushort)(_frequencyRegister & 0x40FF);

    internal void WriteFrequency(ushort value, WriteMask mask)
    {
        _frequencyRegister = (ushort)(MMIO.Merge(_frequencyRegister, value, mask) & 0x40FF);
        LengthEnabled      = (_frequencyRegister & 0x4000) != 0;

        if (mask.IsUpper() && (value & 0x8000) != 0)
            Restart();
    }


    private void Restart()
    {
        Active = _envelope.DACEnabled;

        ReloadEmptyLength();
        _envelope.Restart();

        _lfsr  = NarrowWidth ? 0x40 : 0x4000;
        _high  = false;
        _timer = GetPeriod();
    }

    private int GetPeriod()
    {
        int ratio = _frequencyRegister & 7;
        int shift = (_frequencyRegister >> 4) & 0xF;

        int divisor = ratio == 0 ? 16 : 32 * ratio;
        return divisor << (shift + 1);
    }

    internal override void Tick(int cycles)
    {
        if (!Active)
            return;

        _timer -= cycles;

        while (_timer <= 0)
        {
            _timer += GetPeriod();

            bool carry = (_lfsr & 1) != 0;
            _lfsr >>= 1;

            if (carry)
                _lfsr ^= NarrowWidth ? 0x60 : 0x6000;

            _high = carry;
        }
    }

    internal override void ClockEnvelope() => _envelope.Clock();
}