using System.Runtime.CompilerServices;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (ushort color, bool transparent) SampleTile4bpp(uint tileBase, uint tileIndex, int pixelX, int pixelY, uint paletteBank, uint paletteBase)
    {
        uint tileAddr = tileBase + (tileIndex << 5);
        uint offset   = (uint)(pixelY * 4 + (pixelX >> 1));

        byte packed = _vram.Fetch<byte>(tileAddr + offset);
        byte index  = ((pixelX & 1) == 0) ? (byte)(packed & 0x0F) : (byte)(packed >> 4);

        byte paletteIndex = (byte)((paletteBank << 4) + index);
        ushort color      = _pram.Fetch<ushort>(paletteBase + ((uint)paletteIndex << 1));
        bool transparent  = index == 0;

        return (color, transparent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (ushort color, bool transparent) SampleTile8bpp(uint tileBase, uint tileIndex, int pixelX, int pixelY, uint paletteBase)
    {
        uint tileAddr = tileBase + (tileIndex << 6);
        uint offset   = (uint)(pixelY * 8 + pixelX);

        byte index       = _vram.Fetch<byte>(tileAddr + offset);
        ushort color     = _pram.Fetch<ushort>(paletteBase + ((uint)index << 1));
        bool transparent = index == 0;

        return (color, transparent);
    }
}