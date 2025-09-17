using Trident.Core.Memory.Region;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Core.Machine
{
    public sealed partial class GBA
    {
        public CPUSnapshot CPUSnapshot => CPU.GetSnapshot();

        private IDebugMemory? GetDebugRegion(uint region) => CPU.Bus.GetRegionAsDebug(region);
    }
}