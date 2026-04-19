using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU.Registers;

public struct RegisterSet
{
    [InlineArray(16)]
    private struct RegisterArray
    {
        private uint _r0;
        internal static int Length => 16;
    }

    private readonly byte[] _regLookup =
    [
        0, 1, 2, 3, 4, 10, 11,
        5, 6, 7, 8, 9, 12, 13,
        0, 1, 2, 3, 4, 14, 15,
        0, 1, 2, 3, 4, 16, 17,
        0, 1, 2, 3, 4, 18, 19,
        0, 1, 2, 3, 4, 20, 21,
    ];

    private readonly byte[] _modeToRow = [ 0, 1, 2, 3, 0, 0, 0, 4, 0, 0, 0, 5, 0, 0, 0, 0 ];

    private RegisterArray    _registers;
    private readonly uint[]  _bankStore  = new uint[22];
    private readonly Flags[] _bankedSpsr = new Flags[6];

    public ProcessorMode CurrentMode { get; private set; }
    public Flags CPSR;

    public RegisterSet()
    {
        _registers = new RegisterArray();
        ResetRegisters();
        SwitchMode(ProcessorMode.SYS);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUserOrSystem(ProcessorMode mode) => mode is ProcessorMode.USR or ProcessorMode.SYS;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ModeRow(ProcessorMode mode) => _modeToRow[(int)mode & 0xF];


    internal unsafe ref uint GetRegisterRef(int index)
    {
        Debug.Assert(index >= 0 && index < 16);
        ref uint first = ref Unsafe.AsRef<uint>(Unsafe.AsPointer(ref _registers));
        return ref Unsafe.Add(ref first, index);
    }

    public uint this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetRegisterRef(index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => GetRegisterRef(index) = value;
    }

    public uint this[uint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetRegisterRef((int)index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => GetRegisterRef((int)index) = value;
    }

    internal uint SP
    {
        get => this[13];
        set => this[13] = value;
    }

    internal uint LR
    {
        get => this[14];
        set => this[14] = value;
    }

    public uint PC
    {
        get => this[15];
        set => this[15] = value;
    }


    public void SwitchMode(ProcessorMode newMode)
    {
        if (newMode == CurrentMode)
            return;

        int oldRow  = ModeRow(CurrentMode);
        int newRow  = ModeRow(newMode);
        int oldBase = oldRow * 7;
        int newBase = newRow * 7;

        for (int i = 0; i < 7; i++)
        {
            int phys         = _regLookup[oldBase + i];
            _bankStore[phys] = this[8 + i];
        }

        for (int i = 0; i < 7; i++)
        {
            int phys    = _regLookup[newBase + i];
            this[8 + i] = _bankStore[phys];
        }

        CPSR        = (CPSR & ~(Flags)0x1F) | (Flags)(uint)newMode;
        CurrentMode = newMode;
    }


    public void GetBankForMode(ProcessorMode mode, Span<uint> destination)
    {
        int row       = ModeRow(mode);
        int baseIndex = row * 7;

        int count = mode switch
        {
            ProcessorMode.USR or ProcessorMode.SYS => 7,
            ProcessorMode.FIQ => 7,
            ProcessorMode.IRQ or ProcessorMode.SVC or ProcessorMode.ABT or ProcessorMode.UND => 2,
            _ => 0,
        };

        if (destination.Length != count)
            throw new ArgumentException("Destination span size does not match register count.");

        for (int i = 0; i < count; i++)
        {
            int logical = (mode is ProcessorMode.IRQ or ProcessorMode.SVC or ProcessorMode.ABT or ProcessorMode.UND)
                ? 13 + i
                : 8  + i;

            int lookupIndex = baseIndex + (logical - 8);
            int phys        = _regLookup[lookupIndex];
            destination[i]  = _bankStore[phys];
        }
    }

    public void SetBankForMode(ProcessorMode mode, Span<uint> values)
    {
        int row       = ModeRow(mode);
        int baseIndex = row * 7;

        int count = mode switch
        {
            ProcessorMode.USR or ProcessorMode.SYS => 7,
            ProcessorMode.FIQ => 7,
            ProcessorMode.IRQ or ProcessorMode.SVC or ProcessorMode.ABT or ProcessorMode.UND => 2,
            _ => 0,
        };

        if (values.Length != count)
            throw new ArgumentException($"Expected {count} registers for mode {mode}, got {values.Length}");

        for (int i = 0; i < count; i++)
        {
            int logical = (mode is ProcessorMode.IRQ or ProcessorMode.SVC or ProcessorMode.ABT or ProcessorMode.UND)
                ? 13 + i
                : 8  + i;

            int lookupIndex  = baseIndex + (logical - 8);
            int phys         = _regLookup[lookupIndex];
            _bankStore[phys] = values[i];

            if (mode == CurrentMode)
                this[logical] = values[i];
        }
    }


    public uint GetSPSRForMode(ProcessorMode mode)
    {
        int row = ModeRow(mode);
        return (uint)_bankedSpsr[row];
    }

    public void SetSPSRForMode(ProcessorMode mode, Flags value)
    {
        int row = ModeRow(mode);
        _bankedSpsr[row] = value;
    }

    public Flags SPSR
    {
        get => IsUserOrSystem(CurrentMode) ? CPSR : _bankedSpsr[ModeRow(CurrentMode)];
        set
        {
            if (!IsUserOrSystem(CurrentMode))
                _bankedSpsr[ModeRow(CurrentMode)] = value;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlag(Flags flag) => CPSR |= flag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearFlag(Flags flag) => CPSR &= ~flag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ModifyFlag(Flags flag, bool condition) => CPSR = condition ? (CPSR | flag) : (CPSR & ~flag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsFlagSet(Flags flag) => (CPSR & flag) != 0;



    public void ResetRegisters()
    {
        for (int i = 0; i < RegisterArray.Length; i++)
            this[i] = 0;

        Array.Clear(_bankStore, 0, _bankStore.Length);
        Array.Clear(_bankedSpsr, 0, _bankedSpsr.Length);

        CPSR = (Flags)((0b11 << 6) | (uint)ProcessorMode.SVC); // I and F set; mode SVC
        CurrentMode = ProcessorMode.SVC;
    }

    public ReadOnlySpan<uint> AsSpan()
    {
        return MemoryMarshal.CreateReadOnlySpan
        (
            ref Unsafe.As<RegisterArray, uint>(ref Unsafe.AsRef(ref _registers)),
            RegisterArray.Length
        );
    }
}