using System.Runtime.CompilerServices;

namespace Trident.CodeGeneration.CodeGen
{
    public static class ARMCodeGenUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint GetBit(this uint value, int bit) => value >> bit & 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsBitSet(this uint value, int bit) => (value >> bit & 1) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Extract(this uint value, int hi, int lo) =>
            value >> lo & (1u << hi - lo + 1) - 1;
    }
}
