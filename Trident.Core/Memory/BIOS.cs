using Trident.Core.CPU;
using Trident.Core.Global;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory;

internal sealed class BIOS(Func<uint> getPC, Action<uint> step) : MemoryBase(BIOS.MemorySize, step)
{
    internal const uint MemorySize = 16 * 1024;
    private uint _busValue;

    private readonly Func<uint> _getPC = getPC;

    internal void LoadBIOS(byte[] bios) => _memory.WriteBytes(0, bios);
    internal void Clear() => _memory.Clear();


    public override byte Read8(uint address, PipelineAccess access)    => (byte)Read(address);
    public override ushort Read16(uint address, PipelineAccess access) => (ushort)Read(address.Align<ushort>());
    public override uint Read32(uint address, PipelineAccess access)   => Read(address.Align<uint>());

    public override void Write8(uint address, PipelineAccess access, byte value)    => _step(1);
    public override void Write16(uint address, PipelineAccess access, ushort value) => _step(1);
    public override void Write32(uint address, PipelineAccess access, uint value)   => _step(1);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint Read(uint address)
    {
        _step(1);

        if (address >= 0x4000) return 0x0; // TODO: Open bus

        if (_getPC() < 0x4000)
            _busValue = _memory.Read32(address.Align<uint>());
        else
            Console.WriteLine($"Illegal BIOS read: 0x{address:X8}");

        return _busValue >> ((int)(address & 3) << 3);
    }


    public override T DebugRead<T>(uint address)
    {
        address = address.Align<T>();
        return address < MemorySize ? _memory.Read<T>(address) : default!;
    }

    public override uint BaseAddress => 0x00000000;
    public override uint Length      => MemorySize;
}