namespace Trident.Core.Hardware.Audio.PSG;

internal sealed class Envelope
{
    private int  _initial;
    private int  _period;
    private bool _increase;
    private int  _timer;

    internal int Volume { get; private set; }

    internal bool DACEnabled => _initial != 0 || _increase;

    internal void Configure(ushort register)
    {
        _period   = (register >> 8) & 7;
        _increase = (register & (1 << 11)) != 0;
        _initial  = (register >> 12) & 0xF;
    }

    internal void Restart()
    {
        Volume = _initial;
        _timer = _period;
    }

    internal void Clock()
    {
        if (_period == 0)
            return;

        if (--_timer > 0)
            return;

        _timer = _period;

        if (_increase && Volume < 15)
            Volume++;
        else if (!_increase && Volume > 0)
            Volume--;
    }

    internal void Reset()
    {
        _initial  = 0;
        _period   = 0;
        _increase = false;
        _timer    = 0;
        Volume    = 0;
    }
}