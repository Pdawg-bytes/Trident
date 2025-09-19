using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using Trident.Core.Memory.Region;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.Graphics
{
    internal class PRAM(Action<uint> step) : IMemoryRegion, IDebugMemory
    {
        internal const uint MEMORY_SIZE = 1024;
        private const uint ADDR_MASK = MEMORY_SIZE - 1;
        private readonly UnsafeMemoryBlock _memory = new(MEMORY_SIZE);

        private readonly Action<uint> _step = step;


        public byte Read8(uint address, PipelineAccess access)    => Read<byte>(address);
        public ushort Read16(uint address, PipelineAccess access) => Read<ushort>(address);
        public uint Read32(uint address, PipelineAccess access)   => Read<uint>(address);

        public void Write8(uint address, PipelineAccess access, byte value)    => Write<ushort>(address, (ushort)(value * 0x0101));
        public void Write16(uint address, PipelineAccess access, ushort value) => Write<ushort>(address, value);
        public void Write32(uint address, PipelineAccess access, uint value)   => Write<uint>(address, value);

        public void Dispose() => _memory.Dispose();

        internal void Reset() => _memory.Clear();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T Read<T>(uint address) where T : unmanaged
        {
            bool isWord = Unsafe.SizeOf<T>() == 4;
            _step(isWord ? 2 : 1u);

            return _memory.Read<T>(address.Align<T>() & ADDR_MASK);
        }

        internal T Fetch<T>(uint address) where T : unmanaged => _memory.Read<T>(address.Align<T>() & ADDR_MASK);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write<T>(uint address, T value) where T : unmanaged
        {
            bool isWord = Unsafe.SizeOf<T>() == 4;
            _step(isWord ? 2 : 1u);

            _memory.Write(address.Align<T>() & ADDR_MASK, value);
        }


        public T DebugRead<T>(uint address) where T : unmanaged => _memory.Read<T>(address.Align<T>() & ADDR_MASK);

        public uint BaseAddress => 0x5000000;
        public uint Length => MEMORY_SIZE;
    }
}