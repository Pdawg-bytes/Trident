using System.Runtime.CompilerServices;
using Trident.Core.Global;
using Trident.Core.Hardware.IO;
using static Trident.Core.Hardware.IO.IORegisters;

namespace Trident.Core.Memory
{
    internal class MMIO(HaltControl haltControl)
    {
        private readonly HaltControl _haltControl = haltControl;


        internal MemoryAccessHandler GetAccessHandler()
        {
            return new MemoryAccessHandler
            (
                read8:  (address, _) => Read8(address),
                read16: (address, _) => Read16(address.Align<ushort>()),
                read32: (address, _) => Read32(address.Align<uint>()),

                write8:  (address, _, value) => Write8(address, value),
                write16: (address, _, value) => Write16(address.Align<ushort>(), value),
                write32: (address, _, value) => Write32(address.Align<uint>(), value),

                dispose: null
            );
        }


        private byte Read8(uint address)
        {
            switch (address)
            {
                case HALTCNT: return 0;
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort Read16(uint address) => (ushort)((Read8(address + 1) << 8) | Read8(address));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read32(uint address) => (uint)
        (
            (Read8(address + 0) << 0)  |
            (Read8(address + 1) << 8)  |
            (Read8(address + 2) << 16) |
            (Read8(address + 3) << 24)
        );


        private void Write8(uint address, byte value)
        {
            switch (address)
            {
                case HALTCNT: _haltControl.Write(value); break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write16(uint address, ushort value)
        {
            Write8(address + 0, (byte)(value >> 0));
            Write8(address + 1, (byte)(value >> 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write32(uint address, uint value)
        {
            Write8(address + 0, (byte)(value >> 0 )); 
            Write8(address + 1, (byte)(value >> 8 ));
            Write8(address + 2, (byte)(value >> 16));
            Write8(address + 3, (byte)(value >> 24));
        }
    }
}