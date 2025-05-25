using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Bus;
using Trident.Core.Enums;
using Trident.Core.Global;

namespace Trident.Core.Memory.GamePak.Backup
{
    internal class SRAM
    {
        internal const uint MEMORY_SIZE = 32 * 1024;
        private const uint ADDR_MASK = MEMORY_SIZE - 1;
        private UnsafeMemoryBlock _memory;

        internal SRAM() => _memory = new(MEMORY_SIZE);


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
        internal byte Read8(uint address, PipelineAccess access)
        {
            // Step scheduler based on waitstate settings
            return _memory.Read8(address & ADDR_MASK);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ushort Read16(uint address, PipelineAccess access)
        {
            // Step scheduler based on waitstate settings
            return (ushort)(Read8(address, access) * 0x0101);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint Read32(uint address, PipelineAccess access)
        {
            // Step scheduler based on waitstate settings
            return (uint)(Read8(address, access) * 0x01010101);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write8(uint address, PipelineAccess access, byte value)
        {
            // Step scheduler based on waitstate settings
            _memory.Write8(address & ADDR_MASK, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write16(uint address, PipelineAccess access, ushort value)
        {
            // Step scheduler based on waitstate settings
            Write8(address, access, (byte)(value >> ((ushort)(address & 1) << 3)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write32(uint address, PipelineAccess access, uint value)
        {
            // Step scheduler based on waitstate settings
            Write8(address, access, (byte)(value >> ((int)(address & 3) << 3)));
        }
    }
}
