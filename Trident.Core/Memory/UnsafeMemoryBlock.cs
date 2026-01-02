using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory;

internal unsafe class UnsafeMemoryBlock : IDisposable
{
    private byte* _ptr;
    internal nuint Size { get; }

    internal UnsafeMemoryBlock(nuint size)
    {
        _ptr = (byte*)NativeMemory.AllocZeroed(size);
        Size = size;
    }

    internal void* Pointer => _ptr;

    internal byte Read8(nuint offset) => *(_ptr + offset);
    internal ushort Read16(nuint offset) => *(ushort*)(_ptr + offset);
    internal uint Read32(nuint offset) => *(uint*)(_ptr + offset);

    internal void Write8(nuint offset, byte value) => *(_ptr + offset) = value;
    internal void Write16(nuint offset, ushort value) => *(ushort*)(_ptr + offset) = value;
    internal void Write32(nuint offset, uint value) => *(uint*)(_ptr + offset) = value;

    internal T Read<T>(nuint offset) where T : unmanaged
        => Unsafe.ReadUnaligned<T>(_ptr + offset);

    internal void Write<T>(nuint offset, T value) where T : unmanaged
        => Unsafe.WriteUnaligned<T>(_ptr + offset, value);

    internal void WriteBytes(int address, byte[] data)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(address);
        ArgumentNullException.ThrowIfNull(data);

        if ((long)address + data.Length > (long)Size)
            throw new ArgumentException("Data too large for copy.");

        fixed (byte* src = data)
        {
            Buffer.MemoryCopy(src, _ptr + address, (long)Size - address, data.Length);
        }
    }

    internal void Clear(byte value = 0) => NativeMemory.Fill(_ptr, Size, value);


    public void Dispose()
    {
        if (_ptr != null)
        {
            NativeMemory.Free(_ptr);
            _ptr = null;
        }
    }
}