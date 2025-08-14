using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory
{
    internal class BIOS
    {
        internal const uint MEMORY_SIZE = 16 * 1024;
        private readonly UnsafeMemoryBlock _memory;
        private uint _busValue;

        private readonly Func<uint> _getPC;

        internal BIOS(Func<uint> getPC)
        {
            _memory = new(MEMORY_SIZE);
            _getPC = getPC;
        }

        internal void LoadBIOS(byte[] bios) => _memory.WriteBytes(0, bios);

        internal MemoryAccessHandler GetAccessHandler()
        {
            return new MemoryAccessHandler
            (
                read8:  (address, _) => (byte)Read(address),
                read16: (address, _) => (ushort)Read(address.Align<ushort>()),
                read32: (address, _) => (uint)Read(address.Align<uint>()),

                null,
                null,
                null,

                dispose: _memory.Dispose
            );
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read(uint address)
        {
            // TODO: Step scheduler 1

            if (address >= 0x4000) return 0x0; // TODO: Return open bus; not implemented yet.

            if (_getPC() < 0x4000)
                _busValue = _memory.Read32(address);

            return _busValue >> ((int)(address & 3) << 3);
        }
    }
}