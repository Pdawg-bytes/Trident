using Trident.Core.Global;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using static Trident.Core.Global.ArrayExtensions;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private void RenderObjectLine(uint y)
    {
        Array.Clear(_objWindow);

        if (!DisplayControl.Enable[4] && !DisplayControl.Enable[7]) return;

        for (int obj = 127; obj >= 0; obj--)
        {
            uint oamAddr = (uint)(obj * 8);

            ObjAttr0 attr0 = _oam.Fetch<ObjAttr0>(oamAddr + 0);
            if (!IsSpriteActive(attr0))
                continue;

            ObjAttr1 attr1 = _oam.Fetch<ObjAttr1>(oamAddr + 2);
            ObjAttr2 attr2 = _oam.Fetch<ObjAttr2>(oamAddr + 4);

            var (width, height) = GetSize(attr0.Shape, attr1.Size);

            int objY = attr0.Y;
            if (objY >= 160) objY -= 256;

            bool affine       = attr0.Affine;
            int displayHeight = (affine && attr0.DoubleSize) ? height * 2 : height;

            int spriteY = (int)y - objY;
            if ((uint)spriteY >= displayHeight)
                continue;

            spriteY -= Math.Min(spriteY, Mosaic.ObjYOffset(attr0.Mosaic, (int)y));

            int objX = (int)attr1.X;
            if (objX >= 240) objX -= 512;

            if (affine) RenderAffineSprite(objX, spriteY, width, height, attr0, attr1, attr2);
            else        RenderNormalSprite(objX, spriteY, width, height, attr0, attr1, attr2);
        }
    }

    private bool IsSpriteActive(ObjAttr0 attr0) => attr0.ObjMode switch
    {
        ObjModeNormal or ObjModeSemiTransparent => DisplayControl.Enable[4] && (attr0.Affine || !attr0.Disabled),
        ObjModeWindow                           => DisplayControl.Enable[7] && (attr0.Affine || !attr0.Disabled),
        _                                       => false
    };

    private int SpriteSampleX(ObjAttr0 attr0, int objX, int x)
    {
        int offset = Mosaic.ObjXOffset(attr0.Mosaic, objX + x);
        return x - Math.Min(x, offset);
    }


    private void RenderNormalSprite(int objX, int spriteY, int width, int height, ObjAttr0 attr0, ObjAttr1 attr1, ObjAttr2 attr2)
    {
        if (attr1.VFlip)
            spriteY = height - 1 - spriteY;

        bool use256    = attr0.Color256;
        uint tileIndex = attr2.TileIndex;
        byte priority  = attr2.Priority;
        uint palette   = attr2.Palette;

        int tileY       = spriteY / 8;
        int pixelY      = spriteY & 7;
        int tilesPerRow = width   / 8;

        var (startX, endX) = ComputeXRange(objX, width);

        for (int x = startX; x < endX; x++)
        {
            int sampleX = SpriteSampleX(attr0, objX, x);
            int pixelX  = attr1.HFlip ? width - 1 - sampleX : sampleX;

            int tileX  = pixelX / 8;
            int localX = pixelX & 7;

            uint tile = CalculateTileIndex(tileIndex, tileX, tileY, tilesPerRow, use256);

            if (TrySampleSpritePixel(use256, 0x10000, tile, localX, pixelY, palette, out ushort color))
                WriteOBJPixel(objX + x, color, priority, attr0.ObjMode);
        }
    }

    private void RenderAffineSprite(int objX, int spriteY, int width, int height, ObjAttr0 attr0, ObjAttr1 attr1, ObjAttr2 attr2)
    {
        uint paramBase = attr1.AffineIndex * 32;

        short pa = _oam.Fetch<short>(paramBase + 0x06);
        short pb = _oam.Fetch<short>(paramBase + 0x0E);
        short pc = _oam.Fetch<short>(paramBase + 0x16);
        short pd = _oam.Fetch<short>(paramBase + 0x1E);

        int texWidth  = width;
        int texHeight = height;

        if (attr0.DoubleSize)
        {
            width  *= 2;
            height *= 2;
        }

        int displayCenterX = width     / 2;
        int displayCenterY = height    / 2;
        int texCenterX     = texWidth  / 2;
        int texCenterY     = texHeight / 2;

        int dy = spriteY - displayCenterY;

        bool use256    = attr0.Color256;
        uint tileIndex = attr2.TileIndex;
        byte priority  = attr2.Priority;
        uint palette   = attr2.Palette;

        int tilesPerRow = texWidth / 8;

        var (startX, endX) = ComputeXRange(objX, width);

        for (int x = startX; x < endX; x++)
        {
            int sampleX = SpriteSampleX(attr0, objX, x);

            int dx   = sampleX - displayCenterX;
            int texX = ((pa * dx + pb * dy) >> 8) + texCenterX;
            int texY = ((pc * dx + pd * dy) >> 8) + texCenterY;

            if ((uint)texX >= (uint)texWidth || (uint)texY >= (uint)texHeight)
                continue;

            if (SampleSpriteTexel(texX, texY, tileIndex, tilesPerRow, use256, palette, out ushort color))
                WriteOBJPixel(objX + x, color, priority, attr0.ObjMode);
        }
    }


    private (int start, int end) ComputeXRange(int objX, int width)
    {
        int start = objX < 0 ? -objX : 0;
        int end   = (objX + width > 240) ? 240 - objX : width;
        return (start, end);
    }


    private bool TrySampleSpritePixel(bool use256, uint tileBase, uint tile, int x, int y, uint palette, out ushort color)
    {
        bool transparent;
        if (use256) (color, transparent) = SampleTile8bpp(tileBase, tile, x, y, 0x200u);
        else        (color, transparent) = SampleTile4bpp(tileBase, tile, x, y, palette, 0x200u);

        return !transparent;
    }

    private bool SampleSpriteTexel(int texX, int texY, uint tileIndex, int tilesPerRow, bool use256, uint palette, out ushort color)
    {
        uint tile = CalculateTileIndex(tileIndex, texX / 8, texY / 8, tilesPerRow, use256);
        return TrySampleSpritePixel(use256, 0x10000u, tile, texX & 7, texY & 7, palette, out color);
    }


    private uint CalculateTileIndex(uint baseTileIndex, int tileX, int tileY, int tilesPerRow, bool is256Color)
    {
        if (is256Color)
        {
            uint baseIndex = baseTileIndex / 2;
            return DisplayControl.ObjVramMapping
                ? baseIndex + (uint)(tileY * tilesPerRow + tileX)
                : baseIndex + (uint)(tileY * 16 + tileX);
        }
        else
        {
            return DisplayControl.ObjVramMapping
                ? baseTileIndex + (uint)(tileY * tilesPerRow + tileX)
                : baseTileIndex + (uint)(tileY * 32 + tileX);
        }
    }


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


    private const uint ObjModeNormal          = 0;
    private const uint ObjModeSemiTransparent = 1;
    private const uint ObjModeWindow          = 2;

    private void WriteOBJPixel(int screenX, ushort color, byte priority, uint mode)
    {
        if (mode == ObjModeWindow)
        {
            _objWindow[screenX] = true;
            return;
        }

        ref LayerPixel px = ref GetUnsafe(_objLine, (uint)screenX);
        if (px.IsOpaqueIn(_pixelGeneration) && px.Priority < priority)
            return;

        px = new LayerPixel
        {
            Color           = color,
            Transparent     = false,
            Priority        = priority,
            Source          = OBJLayer,
            SemiTransparent = mode == ObjModeSemiTransparent,
            Generation      = _pixelGeneration
        };
    }
}


[StructLayout(LayoutKind.Explicit, Size = 2)]
internal readonly struct ObjAttr0
{
    [FieldOffset(0)] private readonly ushort _raw;

    [FieldOffset(0)] internal readonly byte Y;
    internal uint ObjMode    => (uint)(_raw >> 10) & 0b11;
    internal bool Affine     => _raw.IsBitSet(8);
    internal bool DoubleSize => _raw.IsBitSet(9);
    internal bool Disabled   => _raw.IsBitSet(9);
    internal uint GfxMode    => (uint)(_raw >> 10) & 0b11;
    internal bool Mosaic     => _raw.IsBitSet(12);
    internal bool Color256   => _raw.IsBitSet(13);
    internal uint Shape      => (uint)(_raw >> 14) & 0b11;
}

[StructLayout(LayoutKind.Explicit, Size = 2)]
internal readonly struct ObjAttr1
{
    [FieldOffset(0)] private readonly ushort _raw;

    internal uint X           => (uint)(_raw & 0x01FF);
    internal uint AffineIndex => (uint)(_raw >> 9) & 0x1F;
    internal bool HFlip       => _raw.IsBitSet(12);
    internal bool VFlip       => _raw.IsBitSet(13);
    internal uint Size        => (uint)(_raw >> 14) & 0b11;
}

[StructLayout(LayoutKind.Explicit, Size = 2)]
internal readonly struct ObjAttr2
{
    [FieldOffset(0)] private readonly ushort _raw;

    internal uint TileIndex => (uint)(_raw & 0x03FF);
    internal byte Priority  => (byte)((_raw >> 10) & 0b11);
    internal uint Palette   => (uint)(_raw >> 12) & 0x0F;
}