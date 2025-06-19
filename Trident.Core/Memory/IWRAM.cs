using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using System.Runtime.CompilerServices;

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
            (
                read8: this.Read8,
                read16: this.Read16,
                read32: this.Read32,

                write8: this.Write8,
                write16: this.Write16,
                write32: this.Write32,

                dispose: _memory.Dispose
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte Read8(uint address, PipelineAccess access)
        {
            return _memory.Read8(address & ADDR_MASK);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort Read16(uint address, PipelineAccess access)
        {
            address = address.Align<ushort>();
            return _memory.Read16(address & ADDR_MASK);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read32(uint address, PipelineAccess access)
        {
            address = address.Align<uint>();
            return _memory.Read32(address & ADDR_MASK);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write8(uint address, PipelineAccess access, byte value)
        {
            _memory.Write8(address & ADDR_MASK, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write16(uint address, PipelineAccess access, ushort value)
        {
            address = address.Align<ushort>();
            _memory.Write16(address & ADDR_MASK, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write32(uint address, PipelineAccess access, uint value)
        {
            address = address.Align<uint>();
            _memory.Write32(address & ADDR_MASK, value);
        }
    }
}