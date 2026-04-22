using Trident.Core.Memory.MappedIO;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Core.Hardware.Interrupts;

internal class InterruptController
{
    private readonly Action _unhaltCPU;
    private readonly Func<bool> _isHalted;

    private bool _globalInterruptEnable;
    private ushort _interruptEnable;
    private ushort _interruptFlag;

    internal bool IRQAvailable { get; private set; }

    internal InterruptController(Action unhaltCPU, Func<bool> isHalted)
    {
        _unhaltCPU = unhaltCPU;
        _isHalted = isHalted;

        Reset();
    }

    internal void Raise(InterruptSource source) => Raise(source, 0);
    internal void Raise(InterruptSource source, int channel = 0)
    {
        if (source is InterruptSource.Timer || source is InterruptSource.DMA)
            _interruptFlag |= (ushort)((ushort)source << channel);
        else
            _interruptFlag |= (ushort)source;

        UpdateIRQStatus();

        if (IRQAvailable && _isHalted())
            _unhaltCPU();
    }


    internal ushort ReadIE() => _interruptEnable;
    internal ushort ReadIF() => _interruptFlag;
    internal ushort ReadIME() => _globalInterruptEnable ? (ushort)1 : (ushort)0;


    internal void WriteIE(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
            _interruptEnable = (ushort)((_interruptEnable & 0xFF00) | (value & 0x00FF));

        if (mask.IsUpper())
            _interruptEnable = (ushort)((_interruptEnable & 0x00FF) | (value & 0x3F00));

        UpdateIRQStatus();
    }

    internal void WriteIF(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
            _interruptFlag &= (ushort)~(value & 0x00FF);

        if (mask.IsUpper())
            _interruptFlag &= (ushort)~(value & 0xFF00);

        UpdateIRQStatus();
    }

    internal void WriteIME(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
        {
            _globalInterruptEnable = (value & 1) != 0;
            UpdateIRQStatus();
        }
    }


    private void UpdateIRQStatus() => IRQAvailable = _globalInterruptEnable && ((_interruptEnable & _interruptFlag) != 0);

    internal void Reset()
    {
        _globalInterruptEnable = false;
        _interruptEnable = 0;
        _interruptFlag = 0;
        IRQAvailable = false;
    }


    internal IRQSnapshot GetSnapshot() => new
    (
        _globalInterruptEnable,
        _interruptEnable,
        _interruptFlag
    );
}


internal enum InterruptSource
{
    LCD_VBlank          = 1 << 0,
    LCD_HBlank          = 1 << 1,
    LCD_VCounterMatch   = 1 << 2,

    Timer               = 1 << 3,

    Serial_DataTransfer = 1 << 7,

    DMA                 = 1 << 8,

    Keypad              = 1 << 12,

    GamePak             = 1 << 13,
}