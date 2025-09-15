using System.Runtime.CompilerServices;
using Trident.Core.CPU.Pipeline;
using Trident.Core.Global;
using Trident.Core.Memory.Region;

namespace Trident.Core.Memory
{
    internal class IWRAM(Action<uint> step) : IMemoryRegion
    {
        internal const uint MEMORY_SIZE = 32 * 1024;
        private const uint ADDR_MASK = MEMORY_SIZE - 1;
        private readonly UnsafeMemoryBlock _memory = new(MEMORY_SIZE);

        private readonly Action<uint> _step = step;


        public byte Read8(uint address, PipelineAccess access) => Read<byte>(address);
        public ushort Read16(uint address, PipelineAccess access) => Read<ushort>(address);
        public uint Read32(uint address, PipelineAccess access) => Read<uint>(address);

        public void Write8(uint address, PipelineAccess access, byte value) => Write<byte>(address, value);
        public void Write16(uint address, PipelineAccess access, ushort value) => Write<ushort>(address, value);
        public void Write32(uint address, PipelineAccess access, uint value) => Write<uint>(address, value);

        public void Dispose() => _memory.Dispose();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T Read<T>(uint address) where T : unmanaged
        {
            _step(1);
            return _memory.Read<T>(address.Align<T>() & ADDR_MASK);
        }

        internal T DebugRead<T>(uint address) where T : unmanaged => _memory.Read<T>(address.Align<T>() & ADDR_MASK);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write<T>(uint address, T value) where T : unmanaged
        {
            _step(1);
            _memory.Write(address.Align<T>() & ADDR_MASK, value);
        }


        internal void Reset() => _memory.Clear();
    }
}