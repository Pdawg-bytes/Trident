using Trident.Core.Global;

namespace Trident.Core.Memory
{
    internal static class EWRAM
    {
        private const uint MEMORY_SIZE = 256 * 1024;
        private const uint ADDR_MASK = MEMORY_SIZE - 1;
        private static UnsafeMemoryBlock _memory = new(MEMORY_SIZE);

        internal static byte Read8(uint address)
        {
            return _memory.Read8(address & ADDR_MASK);
        }

        internal static ushort Read16(uint address)
        {
            address = address.Align<ushort>();
            return _memory.Read16(address & ADDR_MASK);
        }

        internal static uint Read32(uint address)
        {
            address = address.Align<uint>();
            return _memory.Read32(address & ADDR_MASK);
        }

        internal static void Write8(uint address, byte value)
        {
            _memory.Write8(address & ADDR_MASK, value);
        }

        internal static void Write16(uint address, ushort value)
        {
            address = address.Align<ushort>();
            _memory.Write16(address & ADDR_MASK, value);
        }

        internal static void Write32(uint address, uint value)
        {
            address = address.Align<uint>();
            _memory.Write32(address & ADDR_MASK, value);
        }
    }
}