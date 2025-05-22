namespace Trident.Core.Memory
{
    internal unsafe struct MemoryAccessHandler
    {
        internal delegate* managed<uint, byte> Read8;
        internal delegate* managed<uint, ushort> Read16;
        internal delegate* managed<uint, uint> Read32;

        internal delegate* managed<uint, byte, void> Write8;
        internal delegate* managed<uint, ushort, void> Write16;
        internal delegate* managed<uint, uint, void> Write32;
    }
}