using Trident.Core.CPU;
using Trident.Core.Global;
using Trident.Core.Hardware.IO;
using Trident.Core.Hardware.DMA;
using Trident.Core.Hardware.Timers;
using Trident.Core.Hardware.Graphics;
using System.Runtime.CompilerServices;
using Trident.Core.Hardware.Controller;
using Trident.Core.Hardware.Interrupts;

namespace Trident.Core.Memory.MappedIO;

internal partial class MMIO : MemoryBase
{
    private const int RegisterCount = 0x181;
    private readonly RegisterAccessor[] _registers = new RegisterAccessor[RegisterCount];


    private readonly PPU _ppu;

    private readonly DMAManager _dmaManager;

    private readonly TimerManager _timerManager;

    private readonly Keypad _keypad;

    private readonly InterruptController _irqController;

    private readonly WaitControl _waitControl;
    private readonly PostHalt _postHalt;

    internal MMIO
    (
        Action<uint> step,
        PPU ppu,
        DMAManager dmaManager,
        TimerManager timerManager,
        Keypad keypad,
        InterruptController irqController,
        WaitControl waitControl,
        PostHalt postHalt
    ) : base(0, step)
    {
        _ppu           = ppu;
        _dmaManager    = dmaManager;
        _timerManager  = timerManager;
        _keypad        = keypad;
        _irqController = irqController;
        _waitControl   = waitControl;
        _postHalt      = postHalt;

        InitializeRegisterMap();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryNormalize(uint address, out uint index)
    {
        index = (address - 0x04000000) >> 1;
        return index < RegisterCount;
    }

    private ushort Read(uint address)
    {
        if (!TryNormalize(address, out uint index))
            return 0;

        return _registers[index].Read();
    }

    private void Write(uint address, ushort value)
    {
        if (!TryNormalize(address, out uint index))
            return;

        _registers[index].Write(value, WriteMask.Both);
    }


    public override byte Read8(uint address, PipelineAccess access)
    {
        _step(1);

        if (!TryNormalize(address, out uint index))
            return 0;

        int shift = (int)(address & 1) << 3;
        return (byte)(_registers[index].Read() >> shift);
    }

    public override ushort Read16(uint address, PipelineAccess access)
    {
        _step(1);
        return Read(address.Align<ushort>());
    }

    public override uint Read32(uint address, PipelineAccess access)
    {
        _step(1);
        address = address.Align<uint>();
        return (uint)(Read(address) | (Read(address | 2) << 16));
    }


    public override void Write8(uint address, PipelineAccess access, byte value)
    {
        _step(1);

        if (!TryNormalize(address, out uint index))
            return;

        bool upper     = (address & 1) != 0;
        WriteMask mask = upper ? WriteMask.Upper : WriteMask.Lower;

        ushort data = upper ? (ushort)(value << 8) : value;
        _registers[index].Write(data, mask);
    }

    public override void Write16(uint address, PipelineAccess access, ushort value)
    {
        _step(1);
        Write(address.Align<ushort>(), value);
    }

    public override void Write32(uint address, PipelineAccess access, uint value)
    {
        _step(1);
        address = address.Align<uint>();
        Write(address | 0, (ushort)value);
        Write(address | 2, (ushort)(value >> 16));
    }


    public override uint BaseAddress => 0x04000000;
    public override uint Length      => 0x400;
}


[Flags]
internal enum WriteMask : byte
{
    None  = 0,
    Lower = 1 << 0,
    Upper = 1 << 1,
    Both  = Lower | Upper
}

internal static class WriteMaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsLower(this WriteMask mask) => (mask & WriteMask.Lower) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsUpper(this WriteMask mask) => (mask & WriteMask.Upper) != 0;
}