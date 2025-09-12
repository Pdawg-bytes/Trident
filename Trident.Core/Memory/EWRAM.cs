using Trident.Core.Global;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory
{
    internal class EWRAM(Action<uint> step)
    {
        internal const uint MEMORY_SIZE = 256 * 1024;
        private const uint ADDR_MASK = MEMORY_SIZE - 1;
        private readonly UnsafeMemoryBlock _memory = new(MEMORY_SIZE);

        private readonly Action<uint> _step = step;


        internal MemoryAccessHandler GetAccessHandler() => new
        (
            read8:  (address, _) => Read<byte>(address),
            read16: (address, _) => Read<ushort>(address),
            read32: (address, _) => Read<uint>(address),

            write8:  (address, _, value) => Write<byte>(address, value),
            write16: (address, _, value) => Write<ushort>(address, value),
            write32: (address, _, value) => Write<uint>(address, value),

            dispose: _memory.Dispose
        );


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T Read<T>(uint address) where T : unmanaged
        {
            bool isWord = Unsafe.SizeOf<T>() == 4;
            _step(isWord ? 6 : 3u);

            return _memory.Read<T>(address.Align<T>() & ADDR_MASK);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write<T>(uint address, T value) where T : unmanaged
        {
            bool isWord = Unsafe.SizeOf<T>() == 4;
            _step(isWord ? 6 : 3u);

            _memory.Write(address.Align<T>() & ADDR_MASK, value);
        }


        internal void Reset() => _memory.Clear();
    }
}