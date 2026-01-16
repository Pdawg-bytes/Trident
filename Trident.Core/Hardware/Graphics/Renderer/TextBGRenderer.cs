using Trident.Core.Global;
using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private void RenderTextBG(uint id, uint y)
    {
        if (!DisplayControl.Enable[id])
            return;

        Background bg     = Backgrounds[id];
        LayerPixel[] line = _bgLines[id];

        uint blocksWide   = (uint)((bg.Width >> 3) >> 5);
        uint charBaseAddr = bg.CharBaseBlock * 0x4000u;

        for (uint x = 0; x < ScreenWidth; x++)
        {
            uint bgX = (x + bg.XOffset) & (bg.Width  - 1u);
            uint bgY = (y + bg.YOffset) & (bg.Height - 1u);

            uint tileX = bgX >> 3;
            uint tileY = bgY >> 3;

            uint blockX = tileX >> 5;
            uint blockY = tileY >> 5;

            uint screenBlock = bg.ScreenBaseBlock + blockY * blocksWide + blockX;
            uint mapIndex    = ((tileY & 31) << 5) + (tileX & 31);

            ushort entry = _vram.Fetch<ushort>(screenBlock * 0x800u + (mapIndex << 1));

            uint tileIndex = entry & 0x3FFu;
            bool flipX     = entry.IsBitSet(10);
            bool flipY     = entry.IsBitSet(11);
            uint palNum    = (uint)(entry >> 12) & 0x0F;

            uint pixelX = bgX & 7;
            uint pixelY = bgY & 7;

            if (flipX) pixelX ^= 7;
            if (flipY) pixelY ^= 7;

            ushort color;
            bool transparent;

            if (bg.Use256Colors)
            {
                uint tileAddr   = charBaseAddr + (tileIndex << 6);
                uint tileOffset = (pixelY << 3) + pixelX;

                byte paletteIndex = _vram.Fetch<byte>(tileAddr + tileOffset);

                color       = _pram.Fetch<ushort>((uint)(paletteIndex << 1));
                transparent = paletteIndex == 0;
            }
            else
            {
                uint tileAddr   = charBaseAddr + (tileIndex << 5);
                uint rowOffset  = pixelY << 2;
                uint byteOffset = pixelX >> 1;

                byte b   = _vram.Fetch<byte>(tileAddr + rowOffset + byteOffset);
                byte idx = ((pixelX & 1) == 0) ? (byte)(b & 0x0F) : (byte)(b >> 4);

                byte paletteIndex = (byte)((palNum << 4) + idx);

                color       = _pram.Fetch<ushort>((uint)(paletteIndex << 1));
                transparent = idx == 0;
            }

            line[x] = new()
            {
                Color       = color,
                Priority    = bg.Priority,
                Transparent = transparent,
                Source      = (byte)bg.ID
            };
        }
    }
}