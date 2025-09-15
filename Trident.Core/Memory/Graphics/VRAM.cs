using System.Runtime.CompilerServices;
using Trident.Core.CPU.Pipeline;
using Trident.Core.Global;
using Trident.Core.Memory.Region;

namespace Trident.Core.Memory.Graphics
{
    internal class VRAM(Action<uint> step, Func<byte> getDisplayMode) : IMemoryRegion
    {
        internal const uint MEMORY_SIZE = 96 * 1024;
        private const uint ADDR_MASK = MEMORY_SIZE - 1;
        private readonly UnsafeMemoryBlock _memory = new(MEMORY_SIZE);

        private readonly Action<uint> _step = step;
        private readonly Func<byte> _getDisplayMode = getDisplayMode;


        public byte Read8(uint address, PipelineAccess access)    => Read<byte>(address);
        public ushort Read16(uint address, PipelineAccess access) => Read<ushort>(address);
        public uint Read32(uint address, PipelineAccess access)   => Read<uint>(address);

        public void Write8(uint address, PipelineAccess access, byte value)    => Write<ushort>(address, (ushort)(value * 0x0101), true);
        public void Write16(uint address, PipelineAccess access, ushort value) => Write<ushort>(address, value, false);
        public void Write32(uint address, PipelineAccess access, uint value)   => Write<uint>(address, value, false);

        public void Dispose() => _memory.Dispose();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T Read<T>(uint address) where T : unmanaged
        {
            bool isWord = Unsafe.SizeOf<T>() == 4;
            _step(isWord ? 2 : 1u);

            address = address.Align<T>() & 0x1FFFF;
            if (address >= 0x18000) address -= 0x8000;

            return _memory.Read<T>(address);
        }

        internal T Fetch<T>(uint address) where T : unmanaged
        {
            address &= 0x1FFFF;
            if (address >= 0x18000) address -= 0x8000;
            return _memory.Read<T>(address.Align<T>());
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write<T>(uint address, T value, bool isByte) where T : unmanaged
        {
            bool isWord = Unsafe.SizeOf<T>() == 4;
            _step(isWord ? 2 : 1u);

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