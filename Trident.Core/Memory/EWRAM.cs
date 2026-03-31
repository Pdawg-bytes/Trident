using Trident.Core.CPU;
using Trident.Core.Global;
using Trident.Core.Memory.Region;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory;

internal class EWRAM(Action<uint> step) : IMemoryRegion, IDebugMemory
{
    internal const uint MemorySize = 256 * 1024;
    private const uint AddressMask = MemorySize - 1;
    private readonly UnsafeMemoryBlock _memory = new(MemorySize);

    private readonly Action<uint> _step = step;


    public byte Read8(uint address, PipelineAccess access)    => Read<byte>(address);
    public ushort Read16(uint address, PipelineAccess access) => Read<ushort>(address);
    public uint Read32(uint address, PipelineAccess access)   => Read<uint>(address);

    public void Write8(uint address, PipelineAccess access, byte value)    => Write<byte>(address, value);
    public void Write16(uint address, PipelineAccess access, ushort value) => Write<ushort>(address, value);
    public void Write32(uint address, PipelineAccess access, uint value)   => Write<uint>(address, value);

    public void Dispose() => _memory.Dispose();

    internal void Reset() => _memory.Clear();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T Read<T>(uint address) where T : unmanaged
    {
        bool isWord = Unsafe.SizeOf<T>() == 4;
        _step(isWord ? 6 : 3u);

        return _memory.Read<T>(address.Align<T>() & AddressMask);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Write<T>(uint address, T value) where T : unmanaged
    {
        bool isWord = Unsafe.SizeOf<T>() == 4;
        _step(isWord ? 6 : 3u);

        _memory.Write(address.Align<T>() & AddressMask, value);
    }


    public T DebugRead<T>(uint address) where T : unmanaged => _memory.Read<T>(address.Align<T>() & AddressMask);

    public uint BaseAddress => 0x2000000;
    public uint Length => MemorySize;
}