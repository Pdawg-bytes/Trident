using Trident.Core.Global;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.Graphics
{
    internal class VRAM(Action<uint> step, Func<byte> getDisplayMode)
    {
        internal const uint MEMORY_SIZE = 96 * 1024;
        private const uint ADDR_MASK = MEMORY_SIZE - 1;
        private readonly UnsafeMemoryBlock _memory = new(MEMORY_SIZE);

        private readonly Action<uint> _step = step;
        private readonly Func<byte> _getDisplayMode = getDisplayMode;


        internal MemoryAccessHandler GetAccessHandler() => new
        (
            read8: (address, _) => Read<byte>(address),
            read16: (address, _) => Read<ushort>(address),
            read32: (address, _) => Read<uint>(address),

            write8: (address, _, value) => Write<ushort>(address, (ushort)(value * 0x0101), true),
            write16: (address, _, value) => Write<ushort>(address, value, false),
            write32: (address, _, value) => Write<uint>(address, value, false),

            dispose: _memory.Dispose
        );


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T Read<T>(uint address) where T : unmanaged
        {
            bool isWord = Unsafe.SizeOf<T>() == 4;
            _step(isWord ? 2 : (uint)1);

            address = address.Align<T>() & 0x1FFFF;
            if (address >= 0x18000) address -= 0x8000;

            return _memory.Read<T>(address);
        }

        internal T Fetch<T>(uint address) where T : unmanaged => _memory.Read<T>(address & 0xFFFFFFFE);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write<T>(uint address, T value, bool isByte) where T : unmanaged
        {
            bool isWord = Unsafe.SizeOf<T>() == 4;
            _step(isWord ? 2 : (uint)1);

            address = address.Align<T>() & 0x1FFFF;
            if (address >= 0x18000) address -= 0x8000;

            if (isByte)
            {
                bool objWrite = (_getDisplayMode() >= 3) ? address >= 0x14000 : address >= 0x10000;
                if (objWrite) return;
            }
            else
                _memory.Write(address, value);
        }


        internal void Reset() => _memory.Clear();
    }
}