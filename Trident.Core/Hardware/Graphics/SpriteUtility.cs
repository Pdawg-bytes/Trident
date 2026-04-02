using System.Runtime.CompilerServices;

namespace Trident.Core.Hardware.Graphics;

internal static class SpriteUtility
{
    internal static short[] Widths =
    [
        8,  16, 32, 64,
        16, 32, 32, 64,
        8,   8, 16, 32
    ];

    internal static short[] Heights =
    [
        8,  16, 32, 64,
        8,  8,  16, 32,
        16, 32, 32, 64
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (short width, short height) GetSize(uint shape, uint size)
    {
        int index = (int)(shape * 4 + size);
        return (Widths[index], Heights[index]);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint CalculateTileIndex(uint baseTileIndex, int tileX, int tileY, int tilesPerRow, bool is256Color, bool mapping1D)
    {
        if (is256Color)
        {
            uint baseIndex = baseTileIndex >> 1;
            return mapping1D
                ? baseIndex + (uint)(tileY * tilesPerRow + tileX)
                : baseIndex + (uint)(tileY * 16 + tileX);
        }
        else
        {
            return mapping1D
                ? baseTileIndex + (uint)(tileY * tilesPerRow + tileX)
                : baseTileIndex + (uint)(tileY * 32 + tileX);
        }
    }
}