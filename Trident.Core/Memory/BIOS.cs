using Trident.Core.CPU;
using Trident.Core.Global;
using Trident.Core.Memory.Region;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory;

internal sealed class BIOS(Func<uint> getPC, Action<uint> step) : IMemoryRegion, IDebugMemory
{
    internal const uint MemorySize = 16 * 1024;
    private readonly UnsafeMemoryBlock _memory = new(MemorySize);
    private uint _busValue;

    private readonly Func<uint> _getPC = getPC;
    private readonly Action<uint> _step = step;


    internal void LoadBIOS(byte[] bios) => _memory.WriteBytes(0, bios);
    internal void Clear() => _memory.Clear();


    public byte Read8(uint address, PipelineAccess access)    => (byte)Read(address);
    public ushort Read16(uint address, PipelineAccess access) => (ushort)Read(address.Align<ushort>());
    public uint Read32(uint address, PipelineAccess access)   => Read(address.Align<uint>());

    public void Write8(uint address, PipelineAccess access, byte value)    => _step(1);
    public void Write16(uint address, PipelineAccess access, ushort value) => _step(1);
    public void Write32(uint address, PipelineAccess access, uint value)   => _step(1);

    public void Dispose() => _memory.Dispose();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint Read(uint address)
    {
        _step(1);

        if (address >= 0x4000) return 0x0; // TODO: Return open bus; not implemented yet.

        if (_getPC() < 0x4000)
            _busValue = _memory.Read32(address.Align<uint>());
        else
            Console.WriteLine($"Illegal BIOS read: 0x{address:X8}");

        return _busValue >> ((int)(address & 3) << 3);
    }


    public T DebugRead<T>(uint address) where T : unmanaged
    {
        address = address.Align<T>();

        if (address < MemorySize)
            return _memory.Read<T>(address);
        else 
            return default!;
    }

    public uint BaseAddress => 0x00000000;
    public uint Length => MemorySize;
}