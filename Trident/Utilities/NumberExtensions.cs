using System.Runtime.CompilerServices;

namespace Trident.Utilities;

internal static class NumberExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsBitSet(this uint value, int bit) => ((value >> bit) & 1) != 0;
}