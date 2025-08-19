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


        internal ushort ReadKeyControl() => (ushort)(_keyCntMask | (_irqEnabled ? (1 << 14) : 0) | ((int)_irqMode << 15));

        internal void WriteKeyControl(ushort value, bool upper, bool lower)
        {
            if (lower)
            {
                _keyCntMask &= 0xFF00;
                _keyCntMask |= (ushort)(value & 0x00FF);
            }

            if (upper)
            {
                // Only L & R bits (bits 8-9) go in upper byte
                _keyCntMask &= 0x00FF;
                _keyCntMask |= (ushort)(value & 0x0300);

                _irqEnabled = (value & 0x4000) != 0;
                _irqMode = (IRQCondition)((value & 0x8000) >> 15);
            }

            if (ShouldRaiseIRQ()) _raiseIRQ(InterruptSource.Keypad, 0);
        }


        internal ushort ReadKeyInput() => _keyInput;

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