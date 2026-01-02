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
}