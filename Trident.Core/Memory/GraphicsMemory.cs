using Trident.Core.CPU;
using Trident.Core.Global;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory;

internal sealed class PRAM(Action<uint> step) : MemoryBase(PRAM.MemorySize, step)
{
    internal const uint MemorySize = 1024;


    public override byte Read8(uint address, PipelineAccess access)
    {
        _step(1);
        return ReadDirect<byte>(address);
    }

    public override ushort Read16(uint address, PipelineAccess access)
    {
        _step(1);
        return ReadDirect<ushort>(address);
    }

    public override uint Read32(uint address, PipelineAccess access)
    {
        _step(2);
        return ReadDirect<uint>(address);
    }


    public override void Write8(uint address, PipelineAccess access, byte value)
    {
        _step(1);
        WriteDirect(address, (ushort)(value * 0x0101));
    }

    public override void Write16(uint address, PipelineAccess access, ushort value)
    {
        _step(1);
        WriteDirect(address, value);
    }

    public override void Write32(uint address, PipelineAccess access, uint value)
    {
        _step(2);
        WriteDirect(address, value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T Fetch<T>(uint address) where T : unmanaged => ReadDirect<T>(address);

    public override uint BaseAddress => 0x05000000;
    public override uint Length      => MemorySize;
}


internal sealed class OAM(Action<uint> step) : MemoryBase(OAM.MemorySize, step)
{
    internal const uint MemorySize = 1024;


    public override byte Read8(uint address, PipelineAccess access)
    {
        _step(1);
        return ReadDirect<byte>(address);
    }

    public override ushort Read16(uint address, PipelineAccess access)
    {
        _step(1);
        return ReadDirect<ushort>(address);
    }

    public override uint Read32(uint address, PipelineAccess access)
    {
        _step(1);
        return ReadDirect<uint>(address);
    }


    public override void Write8(uint address, PipelineAccess access, byte value)
    {
        _step(1);
    }

    public override void Write16(uint address, PipelineAccess access, ushort value)
    {
        _step(1);
        WriteDirect(address, value);
    }

    public override void Write32(uint address, PipelineAccess access, uint value)
    {
        _step(1);
        WriteDirect(address, value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T Fetch<T>(uint address) where T : unmanaged => ReadDirect<T>(address);

    public override uint BaseAddress => 0x07000000;
    public override uint Length      => MemorySize;
}


internal sealed class VRAM(Action<uint> step) : MemoryBase(VRAM.MemorySize, step)
{
    internal const uint MemorySize = 96 * 1024;
    private Func<uint> _getVRAMBoundary;

    internal void SetGetBoundary(Func<uint> fetch) => _getVRAMBoundary = fetch;


    public override byte Read8(uint address, PipelineAccess access)
    {
        _step(1);
        return ReadInternal<byte>(address);
    }

    public override ushort Read16(uint address, PipelineAccess access)
    {
        _step(1);
        return ReadInternal<ushort>(address);
    }

    public override uint Read32(uint address, PipelineAccess access)
    {
        _step(2);
        return ReadInternal<uint>(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T ReadInternal<T>(uint address) where T : unmanaged
    {
        address = address.Align<T>() & 0x1FFFF;
        if (address >= 0x18000) address -= 0x8000;
        return _memory.Read<T>(address);
    }


    public override void Write8(uint address, PipelineAccess access, byte value)
    {
        _step(1);
        WriteVRAM(address, (ushort)(value * 0x0101), true);
    }

    public override void Write16(uint address, PipelineAccess access, ushort value)
    {
        _step(1);
        WriteVRAM(address, value, false);
    }

    public override void Write32(uint address, PipelineAccess access, uint value)
    {
        _step(2);
        WriteVRAM(address, value, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteVRAM<T>(uint address, T value, bool isByte) where T : unmanaged
    {
        uint boundary = _getVRAMBoundary();
        address = address.Align<T>() & 0x1FFFF;

        if (address >= boundary)
        {
            if (isByte) return;

            if (address >= 0x18000)
            {
                address &= ~0x8000u;
                if (address < boundary) return;
            }
        }
        else if (isByte)
        {
            address = address.Align<ushort>();
        }

        _memory.Write(address, value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T DebugRead<T>(uint address)
    {
        address = address.Align<T>() & 0x1FFFF;
        if (address >= 0x18000) address -= 0x8000;
        return _memory.Read<T>(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T Fetch<T>(uint address) where T : unmanaged
    {
        address = address.Align<T>() & 0x1FFFF;
        if (address >= 0x18000) address -= 0x8000;
        return _memory.Read<T>(address);
    }

    public override uint BaseAddress => 0x06000000;
    public override uint Length      => MemorySize;
}