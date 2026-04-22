using Trident.Core.Global;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using static Trident.Core.Global.ArrayExtensions;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private void RenderTextBG(uint id, uint y)
    {
        if (!DisplayControl.Enable[id])
            return;

        Background bg     = Backgrounds[id];
        LayerPixel[] line = _bgLines[id];

        uint xOffset  = bg.XOffset;
        uint yOffset  = bg.YOffset;
        byte priority = bg.Priority;
        byte source   = (byte)bg.ID;
        bool use256   = bg.Use256Colors;

        var (width, height) = GetTextBGSize(bg.ScreenSize);

        uint charBase = bg.CharBaseBlock * 0x4000u;
        uint bgY      = (y + yOffset) & (height - 1u);

        for (uint x = 0; x < ScreenWidth; x++)
        {
            uint bgX = (x + xOffset) & (width - 1u);

            var (color, transparent) = SampleTextBGTexel(bgX, bgY, bg.ScreenBaseBlock, charBase, use256, width);

            ref LayerPixel px = ref GetUnsafe(line, x);
            px.Color       = color;
            px.Transparent = transparent;
            px.Priority    = priority;
            px.Source      = source;
            px.Generation  = _pixelGeneration;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (ushort color, bool transparent) SampleTextBGTexel(
        uint bgX, uint bgY,
        uint screenBaseBlock, uint charBase, bool use256,
        ushort width)
    {
        uint blocksWide = (uint)((width >> 3) >> 5);

        uint tileY      = bgY >> 3;
        uint blockY     = tileY >> 5;
        uint mapRowBase = (tileY & 0x1F) << 5;

        uint screenBlockRow = screenBaseBlock + blockY * blocksWide;

        uint tileX       = bgX >> 3;
        uint blockX      = tileX >> 5;
        uint screenBlock = screenBlockRow + blockX;
        uint mapIndex    = mapRowBase + (tileX & 0x1F);

        TileEntry entry = _vram.Fetch<TileEntry>(screenBlock * 0x800u + (mapIndex << 1));

        uint pixelX = bgX & 7u;
        if (entry.FlipX) pixelX ^= 7u;

        uint pixelY = bgY & 7u;
        if (entry.FlipY) pixelY ^= 7u;

        if (use256) return SampleTile8bpp(charBase, entry.TileIndex, (int)pixelX, (int)pixelY, 0u);
        else        return SampleTile4bpp(charBase, entry.TileIndex, (int)pixelX, (int)pixelY, entry.Palette, 0u);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (ushort width, ushort height) GetTextBGSize(byte screenSize) => screenSize switch
    {
        0 => (256, 256),
        1 => (512, 256),
        2 => (256, 512),
        3 => (512, 512),
        _ => (0, 0)
    };


    [StructLayout(LayoutKind.Explicit, Size = 2)]
    readonly struct TileEntry
    {
        [FieldOffset(0)] private readonly ushort _raw;

        internal uint TileIndex => (uint)(_raw & 0x03FF);
        internal bool FlipX     => _raw.IsBitSet(10);
        internal bool FlipY     => _raw.IsBitSet(11);
        internal uint Palette   => (uint)(_raw >> 12) & 0x0F;
    }
}