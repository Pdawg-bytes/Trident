using Trident.Core.CPU;
using Trident.Core.Global;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory;

public abstract class MemoryBase(uint memorySize, Action<uint> step) : IDisposable
{
    protected readonly UnsafeMemoryBlock _memory = new(memorySize);
    protected readonly uint _addressMask         = memorySize - 1;
    protected readonly Action<uint> _step        = step;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual byte Read8(uint address, PipelineAccess access)
    {
        ApplyReadTiming(sizeof(byte));
        return ReadDirect<byte>(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual ushort Read16(uint address, PipelineAccess access)
    {
        ApplyReadTiming(sizeof(ushort));
        return ReadDirect<ushort>(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual uint Read32(uint address, PipelineAccess access)
    {
        ApplyReadTiming(sizeof(uint));
        return ReadDirect<uint>(address);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Write8(uint address, PipelineAccess access, byte value)
    {
        ApplyWriteTiming(sizeof(byte));
        WriteDirect(address, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Write16(uint address, PipelineAccess access, ushort value)
    {
        ApplyWriteTiming(sizeof(ushort));
        WriteDirect(address, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Write32(uint address, PipelineAccess access, uint value)
    {
        ApplyWriteTiming(sizeof(uint));
        WriteDirect(address, value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T ReadDirect<T>(uint address) where T : unmanaged
        => _memory.Read<T>(address.Align<T>() & _addressMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void WriteDirect<T>(uint address, T value) where T : unmanaged
        => _memory.Write(address.Align<T>() & _addressMask, value);

    public virtual T DebugRead<T>(uint address) where T : unmanaged
        => _memory.Read<T>(address.Align<T>() & _addressMask);

    internal unsafe void* RawPointer => _memory.Pointer;

    public abstract uint BaseAddress { get; }
    public abstract uint Length      { get; }
    public uint EndAddress => BaseAddress + Length;


    protected virtual void ApplyReadTiming(int accessSize)  => _step(1);
    protected virtual void ApplyWriteTiming(int accessSize) => _step(1);


    public virtual void Dispose() => _memory.Dispose();
    internal virtual void Reset() => _memory.Clear();
}