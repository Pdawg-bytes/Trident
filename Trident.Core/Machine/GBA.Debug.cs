using Trident.Core.Memory;
using Trident.Core.Memory.Region;
using Trident.Core.Debugging.Snapshots;
using Trident.Core.Debugging.Disassembly;
using Trident.Core.Debugging.Breakpoints;

namespace Trident.Core.Machine
{
    public sealed partial class GBA
    {
        internal bool IsDebuggingEnabled => Breakpoints.Enabled;

        public readonly Disassembler Disassembler;
        public readonly BreakpointManager Breakpoints = new();


        public CPUSnapshot GetCPUSnapshot() => CPU.GetSnapshot();

        public IRQSnapshot GetIRQSnapshot() => _irqController.GetSnapshot();

        public DMASnapshot GetDMASnapshot() => _dmaManager.GetSnapshot();


        private IDebugMemory? GetDebugRegion(uint region) => CPU.Bus.GetRegionAsDebug(region);

        public DebugMemoryRead<T> DebugRead<T>(uint address) where T : unmanaged
        {
            IDebugMemory? region = CPU.Bus.GetRegionAsDebug(address >> 24);
            if (region is null || address < region.BaseAddress || address >= region.EndAddress)
            {
                return new DebugMemoryRead<T>
                (
                    value: default,
                    baseAddr: 0,
                    endAddr: 0,
                    isValid: false
                );
            }

            T value = region.DebugRead<T>(address);
            return new DebugMemoryRead<T>
            (
                value: value,
                baseAddr: region.BaseAddress,
                endAddr: region.EndAddress,
                isValid: true
            );
        }
    }
}