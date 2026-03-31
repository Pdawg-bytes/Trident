using Trident.Core.CPU;
using Trident.Core.Global;
using Trident.Core.Memory.Region;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.Graphics;

internal class VRAM(Action<uint> step) : IMemoryRegion, IDebugMemory
{
    internal const uint MemorySize = 96 * 1024;
    private const uint AddressMask = MemorySize - 1;
    private readonly UnsafeMemoryBlock _memory = new(MemorySize);

    private readonly Action<uint> _step = step;
    private Func<uint> _getVRAMBoundary;
    internal void SetGetBoundary(Func<uint> fetch) => _getVRAMBoundary = fetch;

    public byte Read8(uint address, PipelineAccess access)    => Read<byte>(address);
    public ushort Read16(uint address, PipelineAccess access) => Read<ushort>(address);
    public uint Read32(uint address, PipelineAccess access)   => Read<uint>(address);

    public void Write8(uint address, PipelineAccess access, byte value)    => Write<ushort>(address, (ushort)(value * 0x0101), true);
    public void Write16(uint address, PipelineAccess access, ushort value) => Write<ushort>(address, value, false);
    public void Write32(uint address, PipelineAccess access, uint value)   => Write<uint>(address, value, false);

    public void Dispose() => _memory.Dispose();

    internal void Reset() => _memory.Clear();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T Read<T>(uint address) where T : unmanaged
    {
        bool isWord = Unsafe.SizeOf<T>() == 4;
        _step(isWord ? 2 : 1u);

        return ReadInternal<T>(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T Fetch<T>(uint address) where T : unmanaged => ReadInternal<T>(address);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T ReadInternal<T>(uint address) where T : unmanaged
    {
        address = address.Align<T>() & 0x1FFFF;
        if (address >= 0x18000) address -= 0x8000;
        return _memory.Read<T>(address);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Write<T>(uint address, T value, bool isByte) where T : unmanaged
    {
        uint boundary = _getVRAMBoundary();
        address       = address.Align<T>() & 0x1FFFF;

        bool isWord = Unsafe.SizeOf<T>() == 4;
        _step(isWord ? 2 : 1u);

        if (address >= boundary)
        {
            if (isByte) return;

            if (address >= 0x18000)
            {
                address &= ~0x8000u;
                if (address < boundary) return;
            }
        }
        else
        {
            if (isByte)
                address = address.Align<ushort>();
        }

        _memory.Write(address, value);
    }


    public T DebugRead<T>(uint address) where T : unmanaged => _memory.Read<T>(address.Align<T>() & 0x1FFFF);

    public uint BaseAddress => 0x6000000;
    public uint Length => MemorySize;
}