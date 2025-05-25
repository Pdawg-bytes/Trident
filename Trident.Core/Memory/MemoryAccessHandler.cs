using Trident.Core.Enums;

namespace Trident.Core.Memory
{
    internal struct MemoryAccessHandler
    {
        internal Func<uint, PipelineAccess, byte> Read8;
        internal Func<uint, PipelineAccess, ushort> Read16;
        internal Func<uint, PipelineAccess, uint> Read32;

        internal Action<uint, PipelineAccess, byte> Write8;
        internal Action<uint, PipelineAccess, ushort> Write16;
        internal Action<uint, PipelineAccess, uint> Write32;

        internal Action Dispose;
    }
}