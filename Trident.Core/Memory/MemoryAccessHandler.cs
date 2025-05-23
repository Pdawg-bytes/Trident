namespace Trident.Core.Memory
{
    internal unsafe struct MemoryAccessHandler
    {
        internal Func<uint, byte> Read8;
        internal Func<uint, ushort> Read16;
        internal Func<uint, uint> Read32;

        internal Action<uint, byte> Write8;
        internal Action<uint, ushort> Write16;
        internal Action<uint, uint> Write32;

        internal Action Dispose;
    }
}