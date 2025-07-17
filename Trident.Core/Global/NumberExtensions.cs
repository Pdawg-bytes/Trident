using System.Numerics;
using System.Runtime.CompilerServices;

namespace Trident.Core.Global
{
    internal static class NumberExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint GetBit(this uint value, int bit) => (value >> bit) & 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsBitSet(this uint value, int bit) => ((value >> bit) & 1) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsBitSet(this ushort value, int bit) => ((value >> bit) & 1) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Extract(this uint value, int hi, int lo) =>
            (value >> lo) & ((1u << (hi - lo + 1)) - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ExtendFrom(this uint value, int bits)
        {
            int shift = 32 - bits;
            return ((int)(value << shift)) >> shift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint RotateRight(this uint value, int shift) => BitOperations.RotateRight(value, shift);

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


        static readonly uint[] PromotionTable =
        [
            0x00000000, 0x000000FF, 0x0000FF00, 0x0000FFFF,
            0x00FF0000, 0x00FF00FF, 0x00FFFF00, 0x00FFFFFF,
            0xFF000000, 0xFF0000FF, 0xFF00FF00, 0xFF00FFFF,
            0xFFFF0000, 0xFFFF00FF, 0xFFFFFF00, 0xFFFFFFFF
        ];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint BroadcastBits(this uint value) => PromotionTable[value & 0x0F];
    }
}