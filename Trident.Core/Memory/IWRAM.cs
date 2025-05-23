using System.Runtime.CompilerServices;
using Trident.Core.Global;

namespace Trident.Core.Memory
{
    internal class IWRAM
    {
        internal const uint MEMORY_SIZE = 32 * 1024;
        private const uint ADDR_MASK = MEMORY_SIZE - 1;
        private UnsafeMemoryBlock _memory;

        internal IWRAM() => _memory = new(MEMORY_SIZE);


        internal MemoryAccessHandler GetAccessHandler()
        {
            return new MemoryAccessHandler
            {
                Read8 = this.Read8,
                Read16 = this.Read16,
                Read32 = this.Read32,

                Write8 = this.Write8,
                Write16 = this.Write16,
                Write32 = this.Write32,

                Dispose = _memory.Dispose
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal byte Read8(uint address)
        {
            return _memory.Read8(address & ADDR_MASK);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ushort Read16(uint address)
        {
            address = address.Align<ushort>();
            return _memory.Read16(address & ADDR_MASK);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint Read32(uint address)
        {
            address = address.Align<uint>();
            return _memory.Read32(address & ADDR_MASK);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write8(uint address, byte value)
        {
            _memory.Write8(address & ADDR_MASK, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write16(uint address, ushort value)
        {
            address = address.Align<ushort>();
            _memory.Write16(address & ADDR_MASK, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write32(uint address, uint value)
        {
            address = address.Align<uint>();
            _memory.Write32(address & ADDR_MASK, value);
        }
    }
}