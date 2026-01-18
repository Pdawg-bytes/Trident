using System.Runtime.InteropServices;
using Trident.Core.Global;
using Trident.Core.Hardware.Graphics.Registers;

using static Trident.Core.Global.ArrayExtensions;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    readonly struct TileEntry
    {
        [FieldOffset(0)] private readonly ushort _raw;

        internal uint TileIndex => (uint)(_raw & 0x03FF);
        internal bool FlipX     => _raw.IsBitSet(10);
        internal bool FlipY     => _raw.IsBitSet(11);
        internal uint Palette   => (uint)(_raw >> 12) & 0x0F;
    }

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

        uint blocksWide = (uint)((width >> 3) >> 5);
        uint charBase   = bg.CharBaseBlock * 0x4000u;

        uint bgY        = (y + yOffset) & (height - 1u);
        uint tileY      = bgY   >> 3;
        uint blockY     = tileY >> 5;
        uint mapRowBase = (tileY & 0x1F) << 5;

        uint screenBlockRow = bg.ScreenBaseBlock + blockY * blocksWide;

        uint basePixelY = bgY & 7u;

        for (uint x = 0; x < ScreenWidth; x++)
        {
            uint bgX    = (x + xOffset) & (width - 1u);
            uint tileX  = bgX   >> 3;
            uint blockX = tileX >> 5;

            uint screenBlock = screenBlockRow + blockX;
            uint mapIndex    = mapRowBase + (tileX & 0x1F);

            TileEntry entry = _vram.Fetch<TileEntry>(screenBlock * 0x800u + (mapIndex << 1));

            uint pixelX = bgX & 7u;
            if (entry.FlipX) pixelX ^= 7u;

            uint pixelY = basePixelY;
            if (entry.FlipY) pixelY ^= 7u;

            ushort color;
            bool   transparent;

            if (use256)
            {
                uint tileAddr   = charBase + (entry.TileIndex << 6);
                uint tileOffset = (pixelY << 3) + pixelX;

                byte index = _vram.Fetch<byte>(tileAddr + tileOffset);

                color       = _pram.Fetch<ushort>((uint)(index << 1));
                transparent = index == 0;
            }
            else
            {
                uint tileAddr   = charBase + (entry.TileIndex << 5);
                uint rowOffset  = pixelY << 2;
                uint byteOffset = pixelX >> 1;

                byte packed = _vram.Fetch<byte>(tileAddr + rowOffset + byteOffset);
                byte index  = ((pixelX & 1) == 0)
                    ? (byte)(packed & 0x0F)
                    : (byte)(packed >> 4);

                byte paletteIndex = (byte)((entry.Palette << 4) + index);

                color       = _pram.Fetch<ushort>((uint)(paletteIndex << 1));
                transparent = index == 0;
            }

            ref LayerPixel px = ref GetUnsafe(line, x);
            px.Color       = color;
            px.Transparent = transparent;
            px.Priority    = priority;
            px.Source      = source;
            px.Generation  = _pixelGeneration;
        }
    }

    private (ushort width, ushort height) GetTextBGSize(byte screenSize) => screenSize switch
    {
        0 => (256, 256),
        1 => (512, 256),
        2 => (256, 512),
        3 => (512, 512),
        _ => (0, 0)
    };
}