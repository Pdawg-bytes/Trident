using Trident.Core.Hardware.Interrupts;

namespace Trident.Core.Hardware.Controller
{
    internal class Keypad(Action<InterruptSource, int> raiseIRQ)
    {
        private readonly Action<InterruptSource, int> _raiseIRQ = raiseIRQ;

        private ushort _keyInput = 0x03FF; // All keys released

        private ushort _keyCntMask;
        private IRQCondition _irqMode;
        private bool _irqEnabled;


        internal byte ReadKeyControl8(bool upper) => upper ?
            (byte)((_keyCntMask >> 8)     |
                   (_irqEnabled ? 64 : 0) |
                   ((int)_irqMode << 7))  :
            (byte)_keyCntMask;

        internal ushort ReadKeyControl16() => (ushort)(_keyCntMask | (_irqEnabled ? (1 << 14) : 0) | ((int)_irqMode << 15));

        internal void WriteKeyControl8(bool upper, byte value)
        {
            if (upper)
            {
                _keyCntMask &= 0x00FF;
                _keyCntMask |= (ushort)(((value) & 0b11) << 8); // Only write in L & R

                _irqEnabled = (value & 64) != 0;
                _irqMode = (IRQCondition)(value >> 7);
            }
            else
            {
                _keyCntMask &= 0xFF00;
                _keyCntMask |= value;
            }

            if (ShouldRaiseIRQ()) _raiseIRQ(InterruptSource.Keypad, 0);
        }

        internal void WriteKeyControl16(ushort value)
        {
            _keyCntMask = (ushort)(value & 0x03FF);
            _irqEnabled = (value & (1 << 14)) != 0;
            _irqMode    = (IRQCondition)(value >> 15);
        }


        internal byte ReadKeyInput8(bool upper) => upper ? (byte)(_keyInput >> 8) : (byte) _keyInput;
        internal ushort ReadKeyInput16() => _keyInput;

        internal void SetKeyState(GBAKey key, bool pressed)
        {
            if (pressed)
                _keyInput &= (ushort) ~(1 << (int)key);
            else
                _keyInput |= (ushort)  (1 << (int)key);

            if (ShouldRaiseIRQ()) _raiseIRQ(InterruptSource.Keypad, 0);
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
}