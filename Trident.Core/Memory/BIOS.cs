using Trident.Core.Bus;
using Trident.Core.Enums;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory
{
    internal class BIOS
    {
        internal const uint MEMORY_SIZE = 16 * 1024;
        private UnsafeMemoryBlock _memory;
        private uint _busValue;

        private readonly Func<uint> _getPC;

        internal BIOS(Func<uint> getPC)
        {
            _memory = new(MEMORY_SIZE);
            _getPC = getPC;
        }


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
        private byte Read8(uint address, PipelineAccess access) => (byte)Read32(address, access);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort Read16(uint address, PipelineAccess access) => (ushort)Read32(address, access);

        private unsafe uint Read32(uint address, PipelineAccess access)
        {
            if (address >= 0x4000) return 0x0; // Return open bus; not implemented yet.

            int shift = ((int)address & 3) << 3; // Shift amount to extract correct byte out of the word
            if (_getPC() < 0x4000)
                _busValue = _memory.Read32(address & 3);

            return _busValue >> shift;
        }

        internal void LoadBIOS(byte[] bios) => _memory.WriteBytes(0, bios);

        private void Write8(uint address, PipelineAccess access, byte value) => DataBus.InvalidAccess(MemorySection.BIOS, address, true, value);
        private void Write16(uint address, PipelineAccess access, ushort value) => DataBus.InvalidAccess(MemorySection.BIOS, address, true, value);
        private void Write32(uint address, PipelineAccess access, uint value) => DataBus.InvalidAccess(MemorySection.BIOS, address, true, value);
    }
}