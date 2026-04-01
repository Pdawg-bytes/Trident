using Trident.Core.Memory;
using Trident.Core.Debugging.Snapshots;
using Trident.Core.Debugging.Disassembly;
using Trident.Core.Debugging.Breakpoints;

namespace Trident.Core.Machine;

public sealed partial class GBA
{
    internal bool IsDebuggingEnabled => Breakpoints.Enabled;

    public readonly Disassembler Disassembler;
    public readonly BreakpointManager Breakpoints = new();


    public CPUSnapshot GetCPUSnapshot() => CPU.GetSnapshot();

    public IRQSnapshot GetIRQSnapshot() => _irqController.GetSnapshot();

    public DMASnapshot GetDMASnapshot() => _dmaManager.GetSnapshot();


    private MemoryBase? GetDebugRegion(uint region) => CPU.Bus.GetRegionAsDebug(region);

    public DebugMemoryRead<T> DebugRead<T>(uint address) where T : unmanaged
    {
        MemoryBase? region = CPU.Bus.GetRegionAsDebug(address >> 24);
        if (region is null || address < region.BaseAddress || address >= region.EndAddress)
        {
            return new DebugMemoryRead<T>
            (
                Value:       default,
                BaseAddress: 0,
                EndAddress:  0,
                IsValid:     false
            );
        }

        T value = region.DebugRead<T>(address);
        return new DebugMemoryRead<T>
        (
            Value:       value,
            BaseAddress: region.BaseAddress,
            EndAddress:  region.EndAddress,
            IsValid:     true
        );
    }
}

public readonly record struct DebugMemoryRead<T>(T Value, uint BaseAddress, uint EndAddress, bool IsValid) where T : unmanaged;