using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory
{
    internal class BIOS(Func<uint> getPC, Action<uint> step)
    {
        internal const uint MEMORY_SIZE = 16 * 1024;
        private readonly UnsafeMemoryBlock _memory = new(MEMORY_SIZE);
        private uint _busValue;

        private readonly Func<uint> _getPC = getPC;
        private readonly Action<uint> _step = step;


        internal void LoadBIOS(byte[] bios) => _memory.WriteBytes(0, bios);

        internal MemoryAccessHandler GetAccessHandler() => new
        (
            read8:  (address, _) => (byte)Read(address),
            read16: (address, _) => (ushort)Read(address.Align<ushort>()),
            read32: (address, _) => Read(address.Align<uint>()),

            write8:  (_, _, _) => _step(1),
            write16: (_, _, _) => _step(1),
            write32: (_, _, _) => _step(1),

            dispose: _memory.Dispose
        );


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read(uint address)
        {
            _step(1);

            if (address >= 0x4000) return 0x0; // TODO: Return open bus; not implemented yet.

            if (_getPC() < 0x4000)
                _busValue = _memory.Read32(address);
            else
                Console.WriteLine($"Illegal BIOS read: 0x{address:X8}");

            return _busValue >> ((int)(address & 3) << 3);
        }
    }
}