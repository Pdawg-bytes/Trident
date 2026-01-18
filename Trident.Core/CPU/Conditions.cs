using Trident.Core.Bus;
using Trident.Core.CPU.Registers;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU;

public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
{
    // https://github.com/dotnet/runtime/issues/122336
    #if DEBUG
    private ReadOnlySpan<ushort> ConditionLUT => s_conditionLUT;
    private readonly ushort[] s_conditionLUT =
    #else
    private ReadOnlySpan<ushort> ConditionLUT =>
    #endif
    [
        0xF0F0, // EQ: Z == 1
        0x0F0F, // NE: Z == 0
        0xCCCC, // CS: C == 1
        0x3333, // CC: C == 0
        0xFF00, // MI: N == 1
        0x00FF, // PL: N == 0
        0xAAAA, // VS: V == 1
        0x5555, // VC: V == 0
        0x0C0C, // HI: (C == 1) && (Z == 0)
        0xF3F3, // LS: (C == 0) || (Z == 1)
        0xAA55, // GE: N == V
        0x55AA, // LT: N != V
        0x0A05, // GT: (Z == 0) && (N == V)
        0xF5FA, // LE: (Z == 1) || (N != V)
        0xFFFF, // AL: 1
        0x0000  // NV: 0
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ConditionMet(uint condition, Flags cpsr)
    {
        ref ushort lut = ref MemoryMarshal.GetReference(ConditionLUT);

        ushort entry = Unsafe.Add(ref lut, condition);
        int bit      = (int)((uint)cpsr >> 28);

        return ((entry >> bit) & 1) != 0;
    }
}

internal static class Conditions
{
    internal const uint CondEQ = 0b0000;
    internal const uint CondNE = 0b0001;
    internal const uint CondCS = 0b0010;
    internal const uint CondCC = 0b0011;
    internal const uint CondMI = 0b0100;
    internal const uint CondPL = 0b0101;
    internal const uint CondVS = 0b0110;
    internal const uint CondVC = 0b0111;
    internal const uint CondHI = 0b1000;
    internal const uint CondLS = 0b1001;
    internal const uint CondGE = 0b1010;
    internal const uint CondLT = 0b1011;
    internal const uint CondGT = 0b1100;
    internal const uint CondLE = 0b1101;
    internal const uint CondAL = 0b1110;
}