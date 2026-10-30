using Trident.Core.Hardware.Graphics;
using Trident.Core.Memory.MappedIO;

namespace Trident.Core.Hardware.Audio.PSG;

internal sealed class WaveChannel : PSGChannel
{
    private readonly byte[] _waveRam = new byte[32];

    private ushort _controlRegister;
    private ushort _volumeRegister;
    private ushort _frequencyRegister;

    private int _frequency;
    private int _timer;
    private int _position;

    protected override int MaxLength => 256;

    private bool TwoBanks   => (_controlRegister & (1 << 5)) != 0;
    private int  PlayBank   => (_controlRegister >> 6) & 1;
    private bool DacEnabled => (_controlRegister & (1 << 7)) != 0;

    private int CurrentSample
    {
        get
        {
            int offset = PlayBank * 16 + (_position >> 1);
            if (TwoBanks)
                offset &= 0x1F;

            byte data = _waveRam[offset];
            return (_position & 1) == 0 ? data >> 4 : data & 0xF;
        }
    }

    internal override int Output
    {
        get
        {
            if (!Active)
                return 0;

            int sample = CurrentSample;

            if ((_volumeRegister & (1 << 15)) != 0)
                return 8 * (sample * 3 >> 2) - 42;

            return ((_volumeRegister >> 13) & 3) switch
            {
                1 => 8 * sample - 60,
                2 => 8 * (sample >> 1) - 28,
                3 => 8 * (sample >> 2) - 12,
                _ => 0
            };
        }
    }

    internal override void Reset()
    {
        base.Reset();

        _controlRegister   = 0;
        _volumeRegister    = 0;
        _frequencyRegister = 0;
        _frequency         = 0;
        _timer             = 0;
        _position          = 0;
    }



    internal ushort ReadControl() => _controlRegister;

    internal void WriteControl(ushort value, WriteMask mask)
    {
        _controlRegister = (ushort)(MMIO.Merge(_controlRegister, value, mask) & 0x00E0);

        if (!DacEnabled)
            Active = false;
    }


    internal ushort ReadVolume() => (ushort)(_volumeRegister & 0xE000);

    internal void WriteVolume(ushort value, WriteMask mask)
    {
        _volumeRegister = (ushort)(MMIO.Merge(_volumeRegister, value, mask) & 0xE0FF);

        if (mask.IsLower())
            LoadLength(value & 0xFF);
    }


    internal ushort ReadFrequency() => (ushort)(_frequencyRegister & 0x4000);

    internal void WriteFrequency(ushort value, WriteMask mask)
    {
        _frequencyRegister = (ushort)(MMIO.Merge(_frequencyRegister, value, mask) & 0x47FF);
        _frequency         = _frequencyRegister & 0x7FF;
        LengthEnabled      = (_frequencyRegister & 0x4000) != 0;

        if (mask.IsUpper() && (value & 0x8000) != 0)
            Restart();
    }


    internal ushort ReadWaveRAM(int index)
    {
        int offset = (PlayBank ^ 1) * 16 + index * 2;
        return (ushort)(_waveRam[offset] | (_waveRam[offset + 1] << 8));
    }

    internal void WriteWaveRAM(int index, ushort value, WriteMask mask)
    {
        int offset = (PlayBank ^ 1) * 16 + index * 2;

        if (mask.IsLower())
            _waveRam[offset] = (byte)value;

        if (mask.IsUpper())
            _waveRam[offset + 1] = (byte)(value >> 8);
    }



    private void Restart()
    {
        Active = DacEnabled;

        ReloadEmptyLength();

        _timer    = GetPeriod();
        _position = 0;
    }

    private int GetPeriod() => (2048 - _frequency) * 8;

    internal override void Tick(int cycles)
    {
        if (!Active)
            return;

        _timer -= cycles;

        int mask = TwoBanks ? 63 : 31;

        while (_timer <= 0)
        {
            _timer    += GetPeriod();
            _position  = (_position + 1) & mask;
        }
    }
}