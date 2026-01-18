using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Trident.Core.Global;

internal static class ArrayExtensions
{
    internal static bool ContainsAscii(this byte[] arr, string str)
    {
        if (str.Length == 0 || arr.Length < str.Length)
            return false;

        byte[] needleBytes = Encoding.ASCII.GetBytes(str);
        byte firstByte = needleBytes[0];
        int maxIndex = arr.Length - needleBytes.Length;

        bool CheckMatches(int offset) =>
            offset <= maxIndex && arr[offset] == firstByte && needleBytes.AsSpan(1).SequenceEqual(arr.AsSpan(offset + 1, needleBytes.Length - 1));

        for (int i = 0; i <= maxIndex; i++)
            if (CheckMatches(i)) return true;

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