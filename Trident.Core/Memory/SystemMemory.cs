using Trident.Core.CPU;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory;

internal sealed class IWRAM(Action<uint> step) : MemoryBase(IWRAM.MemorySize, step)
{
    internal const uint MemorySize   = 32 * 1024;
    public override uint BaseAddress => 0x03000000;
    public override uint Length      => MemorySize;
}


internal sealed class EWRAM(Action<uint> step) : MemoryBase(EWRAM.MemorySize, step)
{
    internal const uint MemorySize   = 256 * 1024;
    public override uint BaseAddress => 0x02000000;
    public override uint Length      => MemorySize;

    protected override void ApplyReadTiming(int accessSize)  => _step(accessSize == 4 ? 6u : 3u);
    protected override void ApplyWriteTiming(int accessSize) => _step(accessSize == 4 ? 6u : 3u);
}


internal sealed class UnusedSection(Action<uint> step) : MemoryBase(0, step)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte Read8(uint address, PipelineAccess access)
    {
        _step(1);
        return 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ushort Read16(uint address, PipelineAccess access)
    {
        _step(1);
        return 0xFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override uint Read32(uint address, PipelineAccess access)
    {
        _step(1);
        return 0xFFFFFFFF;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write8(uint address, PipelineAccess access, byte value) => _step(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write16(uint address, PipelineAccess access, ushort value) => _step(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write32(uint address, PipelineAccess access, uint value) => _step(1);


    public override void Dispose() { }

    public override uint BaseAddress => 0;
    public override uint Length      => 0;
}