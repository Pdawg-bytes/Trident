using static Trident.Core.Hardware.IO.IORegisters;

namespace Trident.Core.Hardware.Interrupts
{
    internal class InterruptController
    {
        private readonly Action _unhaltCPU;

        private bool _globalInterruptEnable;
        private ushort _interruptEnable;
        private ushort _interruptFlag;

        internal bool IRQAvailable { get; private set; }

        internal InterruptController(Action unhaltCPU)
        {
            _unhaltCPU = unhaltCPU;
            Reset();
        }


        internal void Raise(InterruptSource source, int channel = 0)
        {
            if (source is InterruptSource.Timer || source is InterruptSource.DMA)
                _interruptFlag |= (ushort)((ushort)source << channel);
            else
                _interruptFlag |= (ushort)source;

            UpdateIRQStatus();

            if (IRQAvailable)
                _unhaltCPU();
        }


        internal byte Read8(uint address) => address switch
        {
            IE     => (byte)(_interruptEnable & 0xFF),
            IE + 1 => (byte)(_interruptEnable >> 8),
            IF     => (byte)(_interruptFlag & 0xFF),
            IF + 1 => (byte)(_interruptFlag >> 8),
            IME    => _globalInterruptEnable ? (byte)1 : (byte)0,
            _ => 0
        };

        internal ushort Read16(uint address) => address switch
        {
            IE  => _interruptEnable,
            IF  => _interruptFlag,
            IME => _globalInterruptEnable ? (ushort)1 : (ushort)0,
            _ => 0
        };

        internal void Write8(uint address, byte value)
        {
            switch (address)
            {
                case IE:
                    // Bits 14-15 are not used as interrupt sources, so mask out the top 2 bits.
                    _interruptEnable &= 0x3F00;
                    _interruptEnable |= value;
                    break;
                case IE + 1:
                    _interruptEnable &= 0x00FF;
                    _interruptFlag |= value;
                    break;

                case IF:
                    _interruptFlag &= (byte)~value;
                    break;
                case IF + 1:
                    _interruptFlag &= (byte)~(value << 8);
                    break;

                case IME:
                    _globalInterruptEnable = (value & 1) != 0;
                    break;
            }

            UpdateIRQStatus();
        }

        internal void Write16(uint address, ushort value)
        {
            switch (address)
            {
                case IE:
                    _interruptEnable = (ushort)(value & 0x3FFF);
                    break;

                case IF:
                    _interruptFlag &= (ushort)~value;
                    break;

                case IME:
                    _globalInterruptEnable = (value & 1) != 0;
                    break;
            }

            UpdateIRQStatus();
        }


        private void UpdateIRQStatus() => IRQAvailable = _globalInterruptEnable && ((_interruptEnable & _interruptFlag) != 0);

        internal void Reset()
        {
            _globalInterruptEnable = false;
            _interruptEnable = 0;
            _interruptFlag = 0;
            IRQAvailable = false;
        }
    }
}