using Trident.Core.CPU;

namespace Trident.Core.Debugging.Snapshots
{
    public readonly struct CPUSnapshot(IReadOnlyList<uint> Registers, uint CPSR, uint SPSR, PrivilegeMode Mode)
    {
        public readonly IReadOnlyList<uint> Registers = Registers;

        public readonly uint CPSR = CPSR;
        public readonly uint SPSR = SPSR;
        public readonly PrivilegeMode Mode = Mode;
    }
}