using Trident.Core.Memory.MappedIO;
using Trident.Core.Hardware.Interrupts;

namespace Trident.Core.Hardware.Controller;

internal class Keypad(Action<InterruptSource> raiseIRQ)
{
    private readonly Action<InterruptSource> _raiseIRQ = raiseIRQ;

    private ushort _keyInput = 0x03FF; // All keys released

    private ushort _keyCntMask;
    private IRQCondition _irqMode;
    private bool _irqEnabled;


    internal ushort ReadKeyControl() => (ushort)(_keyCntMask | (_irqEnabled ? (1 << 14) : 0) | ((int)_irqMode << 15));

    internal void WriteKeyControl(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
        {
            _keyCntMask &= 0xFF00;
            _keyCntMask |= (ushort)(value & 0x00FF);
        }

        if (mask.IsUpper())
        {
            _keyCntMask &= 0x00FF;
            _keyCntMask |= (ushort)(value & 0x0300);

            _irqEnabled = (value & 0x4000) != 0;
            _irqMode = (IRQCondition)((value & 0x8000) >> 15);
        }

        if (ShouldRaiseIRQ()) _raiseIRQ(InterruptSource.Keypad);
    }


    internal ushort ReadKeyInput() => _keyInput;

    internal void SetKeyState(GBAKey key, bool pressed)
    {
        if (pressed)
            _keyInput &= (ushort) ~(1 << (int)key);
        else
            _keyInput |= (ushort)  (1 << (int)key);

        if (ShouldRaiseIRQ()) _raiseIRQ(InterruptSource.Keypad);
    }


    private bool ShouldRaiseIRQ()
    {
        if (!_irqEnabled) return false;

        int negatedInput = ~_keyInput;

        if (_irqMode == IRQCondition.OR)
            return (negatedInput & _keyCntMask) != 0;
        else 
            return (negatedInput & _keyCntMask) == _keyCntMask;
    }

    enum IRQCondition
    {
        OR,
        AND
    }
}


[Flags]
public enum GBAKey : ushort
{
    A      = 0,
    B      = 1,
    Select = 2,
    Start  = 3,
    Right  = 4,
    Left   = 5,
    Up     = 6,
    Down   = 7,
    RB     = 8,
    LB     = 9
}