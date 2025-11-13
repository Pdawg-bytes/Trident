using Trident.Core.Memory;
using Trident.Core.Memory.Region;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Core.Machine
{
    public sealed partial class GBA
    {
        public CPUSnapshot CPUSnapshot => CPU.GetSnapshot();

        public IRQSnapshot IRQSnapshot => _irqController.GetSnapshot();

        public DMASnapshot DMASnapshot => _dmaManager.GetSnapshot();


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