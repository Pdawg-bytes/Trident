using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Trident.Core.Global;

internal static class ArrayExtensions
{
    internal static bool ContainsAscii(this ReadOnlySpan<byte> data, string needle, int step)
    {
        if (needle.Length == 0 || data.Length < needle.Length)
            return false;

        ReadOnlySpan<byte> needleBytes = System.Text.Encoding.ASCII.GetBytes(needle);

        int maxIndex = data.Length - needle.Length;

        for (int i = 0; i <= maxIndex; i += step)
        {
            if (data[i] != needle[0]) continue;

            if (needleBytes[1..].SequenceEqual(data[(i + 1)..(i + needle.Length)]))
                return true;
        }

        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T GetUnsafe<T>(T[] array, uint index)
        where T : unmanaged
    {
        ref T start = ref MemoryMarshal.GetArrayDataReference(array);
        return ref Unsafe.Add(ref start, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetUnsafe<T>(T[] array, uint index, T value)
        where T : unmanaged
    {
        ref T start = ref MemoryMarshal.GetArrayDataReference(array);
        Unsafe.Add(ref start, index) = value;
    }
}