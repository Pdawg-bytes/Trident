using Trident.Core.CPU.Pipeline;

namespace Trident.Core.Memory
{
    public readonly struct MemoryAccessHandler(
        Func<uint, PipelineAccess, byte> read8,
        Func<uint, PipelineAccess, ushort> read16,
        Func<uint, PipelineAccess, uint> read32,

        Action<uint, PipelineAccess, byte> write8,
        Action<uint, PipelineAccess, ushort> write16,
        Action<uint, PipelineAccess, uint> write32,

        Action dispose
        )
    {
        internal readonly Func<uint, PipelineAccess, byte> Read8 = read8;
        internal readonly Func<uint, PipelineAccess, ushort> Read16 = read16;
        internal readonly Func<uint, PipelineAccess, uint> Read32 = read32;

        internal readonly Action<uint, PipelineAccess, byte> Write8 = write8;
        internal readonly Action<uint, PipelineAccess, ushort> Write16 = write16;
        internal readonly Action<uint, PipelineAccess, uint> Write32 = write32;

        internal readonly Action Dispose = dispose;
    }
}