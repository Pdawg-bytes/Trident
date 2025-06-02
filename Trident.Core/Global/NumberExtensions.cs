using System.Numerics;
using System.Runtime.CompilerServices;

namespace Trident.Core.Global
{
    internal static class NumberExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint GetBit(this uint value, int bit) => (value >> bit) & 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsBitSet(this uint value, byte bit) => ((value >> bit) & 1) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Extract(this uint value, int hi, int lo) =>
            (value >> lo) & ((1u << (hi - lo + 1)) - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Extend(this uint value, int bits)
        {
            int shift = 32 - bits;
            return (value << shift) >> shift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint NearestPow2(this uint value)
        {
            if (value == 0) return 1;

            uint upper = 1u << (32 - BitOperations.LeadingZeroCount(value - 1));
            uint lower = upper >> 1;

            return (value - lower < upper - value) ? lower : upper;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Align<T>(this uint value) where T : unmanaged =>
            value & ~(uint)(Unsafe.SizeOf<T>() - 1);
    }
}