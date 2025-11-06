using Trident.Core.CPU;

namespace Trident.Core.Debugging.Snapshots
{
    public unsafe struct CPUSnapshot
    {
        private fixed uint _registers[16];

        public readonly uint CPSR;
        public readonly uint SPSR;
        public readonly ProcessorMode Mode;

        public CPUSnapshot(ReadOnlySpan<uint> registers, uint cpsr, uint spsr, ProcessorMode mode)
        {
            if (registers.Length != 16)
                throw new ArgumentException("Must provide exactly 16 registers", nameof(registers));

            for (int i = 0; i < 16; i++)
                _registers[i] = registers[i];

            CPSR = cpsr;
            SPSR = spsr;
            Mode = mode;
        }

        public ReadOnlySpan<uint> Registers
        {
            get
            {
                fixed (uint* ptr = _registers)
                    return new ReadOnlySpan<uint>(ptr, 16);
            }
        }
    }
}