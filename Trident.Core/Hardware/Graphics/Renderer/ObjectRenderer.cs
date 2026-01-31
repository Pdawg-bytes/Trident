using Trident.Core.Global;
using System.Runtime.InteropServices;

using static Trident.Core.Global.ArrayExtensions;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private void RenderObjectLine(uint y)
    {
        if (!DisplayControl.Enable[4]) return;

        _objDrawCycles = DisplayControl.HBlankIntervalFree ? 954 : 1210;

        for (int obj = 127; obj >= 0; obj--)
        {
            uint oamAddr = (uint)(obj * 8);

            ObjAttr0 attr0 = _oam.Fetch<ObjAttr0>(oamAddr + 0);
            if (attr0.ObjMode == 2) continue;

            ObjAttr1 attr1 = _oam.Fetch<ObjAttr1>(oamAddr + 2);
            ObjAttr2 attr2 = _oam.Fetch<ObjAttr2>(oamAddr + 4);

            var (width, height) = GetUnsafe(SpriteSizes, attr0.Shape * 4 + attr1.Size);

            int objY = attr0.Y;
            if (objY >= 160) objY -= 256;

            bool affine       = attr0.Affine;
            int displayHeight = (affine && attr0.DoubleSize) ? height * 2 : height;

            int spriteY = (int)y - objY;
            if ((uint)spriteY >= displayHeight)
                continue;

            int objX = (int)attr1.X;
            if (objX >= 240) objX -= 512;

            if (affine) RenderAffineSprite(objX, objY, (int)y, width, height, attr0, attr1, attr2);
            else        RenderNormalSprite(objX, spriteY, width, height, attr0, attr1, attr2);
        }
    }


    private void RenderNormalSprite(int objX, int spriteY, int width, int height, ObjAttr0 attr0, ObjAttr1 attr1, ObjAttr2 attr2)
    {
        if (attr1.VFlip)
            spriteY = height - 1 - spriteY;

        bool use256    = attr0.Color256;
        uint tileBase  = 0x10000u;
        uint tileIndex = attr2.TileIndex;
        byte priority  = attr2.Priority;
        uint palette   = attr2.Palette;

        int tileY       = spriteY >> 3;
        int pixelY      = spriteY & 7;
        int tilesPerRow = width >> 3;

        var (startX, endX) = ComputeXRange(objX, width);

        for (int x = startX; x < endX; x++)
        {
            int pixelX = attr1.HFlip ? width - 1 - x : x;

            int tileX = pixelX >> 3;
            int localX = pixelX & 7;

            uint tile = CalculateSpriteTileIndex(tileIndex, tileX, tileY, tilesPerRow, use256);

            if (TrySampleSpritePixel(use256, tileBase, tile, localX, pixelY, palette, out ushort color))
            {
                int screenX = objX + x;

                if ((uint)screenX < 240)
                    WriteObjPixel(screenX, color, priority);
            }
        }
    }

    private void RenderAffineSprite(int objX, int objY, int screenY, int width, int height, ObjAttr0 attr0, ObjAttr1 attr1, ObjAttr2 attr2)
    {
        uint affineIndex = attr1.AffineIndex;
        uint paramBase = affineIndex * 32;

        short pa = _oam.Fetch<short>(paramBase + 0x06);
        short pb = _oam.Fetch<short>(paramBase + 0x0E);
        short pc = _oam.Fetch<short>(paramBase + 0x16);
        short pd = _oam.Fetch<short>(paramBase + 0x1E);

        int texWidth = width;
        int texHeight = height;

        if (attr0.DoubleSize)
        {
            width  <<= 1;
            height <<= 1;
        }

        int displayCenterX = width     >> 1;
        int displayCenterY = height    >> 1;
        int texCenterX     = texWidth  >> 1;
        int texCenterY     = texHeight >> 1;

        int dy = screenY - (objY + displayCenterY);

        bool use256    = attr0.Color256;
        uint tileBase  = 0x10000u;
        uint tileIndex = attr2.TileIndex;
        byte priority  = attr2.Priority;
        uint palette   = attr2.Palette;

        int tilesPerRow = texWidth >> 3;

        var (startX, endX) = ComputeXRange(objX, width);

        for (int x = startX; x < endX; x++)
        {
            int dx   = x - displayCenterX;
            int texX = ((pa * dx + pb * dy) >> 8) + texCenterX;
            int texY = ((pc * dx + pd * dy) >> 8) + texCenterY;

            if ((uint)texX >= texWidth || (uint)texY >= texHeight)
                continue;

            int tileX  = texX >> 3;
            int tileY  = texY >> 3;
            int localX = texX & 7;
            int localY = texY & 7;

            uint tile = CalculateSpriteTileIndex(tileIndex, tileX, tileY, tilesPerRow, use256);

            if (TrySampleSpritePixel(use256, tileBase, tile, localX, localY, palette, out ushort color))
            {
                int screenX = objX + x;

                if ((uint)screenX < 240)
                    WriteObjPixel(screenX, color, priority);
            }
        }
    }


    private (int start, int end) ComputeXRange(int objX, int width)
    {
        int start = objX < 0 ? -objX : 0;
        int end = (objX + width > 240) ? 240 - objX : width;
        return (start, end);
    }

    private bool TrySampleSpritePixel(bool use256, uint tileBase, uint tile, int x, int y, uint palette, out ushort color)
    {
        bool transparent;
        if (use256) (color, transparent) = SampleTile8bpp(tileBase, tile, x, y, 0x200u);
        else        (color, transparent) = SampleTile4bpp(tileBase, tile, x, y, palette, 0x200u);

        return !transparent;
    }

    private void WriteObjPixel(int screenX, ushort color, byte priority)
    {
        ref LayerPixel px = ref GetUnsafe(_objLine, (uint)screenX);
        px.Color       = color;
        px.Transparent = false;
        px.Priority    = priority;
        px.Source      = 4;
        px.Generation  = _pixelGeneration;
    }


    private readonly (int width, int height)[] SpriteSizes =
    [
        (8, 8),  (16, 16), (32, 32), (64, 64),
        (16, 8), (32, 8),  (32, 16), (64, 32),
        (8, 16), (8, 32),  (16, 32), (32, 64)
    ];


    [StructLayout(LayoutKind.Explicit, Size = 2)]
    readonly struct ObjAttr0
    {
        [FieldOffset(0)] private readonly ushort _raw;

        [FieldOffset(0)] internal readonly byte Y;
        internal uint ObjMode    => (uint)(_raw >> 8) & 0b11;
        internal bool Affine     => _raw.IsBitSet(8);
        internal bool DoubleSize => _raw.IsBitSet(9);
        internal uint GfxMode    => (uint)(_raw >> 10) & 0b11;
        internal bool Mosaic     => _raw.IsBitSet(12);
        internal bool Color256   => _raw.IsBitSet(13);
        internal uint Shape      => (uint)(_raw >> 14) & 0b11;
    }

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    readonly struct ObjAttr1
    {
        [FieldOffset(0)] private readonly ushort _raw;

        internal uint X           => (uint)(_raw & 0x01FF);
        internal uint AffineIndex => (uint)(_raw >> 9) & 0x1F;
        internal bool HFlip       => _raw.IsBitSet(12);
        internal bool VFlip       => _raw.IsBitSet(13);
        internal uint Size        => (uint)(_raw >> 14) & 0b11;
    }

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    readonly struct ObjAttr2
    {
        [FieldOffset(0)] private readonly ushort _raw;

        internal uint TileIndex => (uint)(_raw & 0x03FF);
        internal byte Priority  => (byte)((_raw >> 10) & 0b11);
        internal uint Palette   => (uint)(_raw >> 12) & 0x0F;
    }
}