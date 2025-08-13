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
            0 => (byte)(_interruptEnable & 0xFF),
            1 => (byte)(_interruptEnable >> 8),
            2 => (byte)(_interruptFlag & 0xFF),
            3 => (byte)(_interruptFlag >> 8),
            4 => _globalInterruptEnable ? (byte)1 : (byte)0,
            _ => 0
        };

        internal ushort Read16(uint address) => address switch
        {
            0 => _interruptEnable,
            2 => _interruptFlag,
            4 => _globalInterruptEnable ? (ushort)1 : (ushort)0,
            _ => 0
        };

        internal void Write8(uint address, byte value)
        {
            switch (address)
            {
                case 0:
                    // Bits 14-15 are not used as interrupt sources, so mask out the top 2 bits.
                    _interruptEnable &= 0x3F00;
                    _interruptEnable |= value;
                    break;
                case 1:
                    _interruptEnable &= 0x00FF;
                    _interruptFlag |= value;
                    break;

                case 2:
                    _interruptFlag &= (byte)~value;
                    break;
                case 3:
                    _interruptFlag &= (byte)~(value << 8);
                    break;

                case 4:
                    _globalInterruptEnable = (value & 1) != 0;
                    break;
            }

            UpdateIRQStatus();
        }

        internal void Write16(uint address, ushort value)
        {
            switch (address)
            {
                case 0:
                    _interruptEnable = (ushort)(value & 0x3FFF);
                    break;

                case 2:
                    _interruptFlag &= (ushort)~value;
                    break;

                case 4:
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