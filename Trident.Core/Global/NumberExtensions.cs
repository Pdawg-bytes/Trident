using System.Runtime.CompilerServices;

namespace Trident.Core.Global
{
    internal static class NumberExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint GetBit(this uint value, int bit) => (value >> bit) & 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Extract(this uint value, int hi, int lo) =>
            (value >> lo) & ((1u << (hi - lo + 1)) - 1);

        public static uint Extend(this uint value, int bits)
        {
            int shift = 32 - bits;
            return (value << shift) >> shift;
        }
    }
}