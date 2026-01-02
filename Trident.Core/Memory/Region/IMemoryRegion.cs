using Trident.Core.CPU.Pipeline;

namespace Trident.Core.Memory.Region;

internal interface IMemoryRegion
{
    byte Read8(uint address, PipelineAccess access);
    ushort Read16(uint address, PipelineAccess access);
    uint Read32(uint address, PipelineAccess access);

    void Write8(uint address, PipelineAccess access, byte value);
    void Write16(uint address, PipelineAccess access, ushort value);
    void Write32(uint address, PipelineAccess access, uint value);

    void Dispose();
}