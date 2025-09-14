using Trident.Core.CPU;

namespace Trident.Core.Debugging.Snapshots
{
    public readonly struct CPUSnapshot(IReadOnlyList<uint> registers, uint cpsr, uint spsr, ProcessorMode mode)
    {
        public readonly IReadOnlyList<uint> Registers = registers;

        public readonly uint CPSR = cpsr;
        public readonly uint SPSR = spsr;
        public readonly ProcessorMode Mode = mode;
    }
}