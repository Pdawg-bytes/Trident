using Trident.Core.Memory.MappedIO;

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

        internal void Raise(InterruptSource source) => Raise(source, 0);
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
    }
}