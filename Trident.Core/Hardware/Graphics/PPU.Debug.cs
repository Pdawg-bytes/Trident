using Trident.Core.Debugging.Snapshots;
using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    internal BackgroundSnapshot GetBackgroundSnapshot() => new
    (
        DisplayControl.BackgroundMode,
        DisplayControl.Enable[0],
        DisplayControl.Enable[1],
        DisplayControl.Enable[2],
        DisplayControl.Enable[3],
        DisplayControl.Enable[4],
        DisplayControl.ForcedBlank,
        MakeBGLayerSnapshot(Backgrounds[0]),
        MakeBGLayerSnapshot(Backgrounds[1]),
        MakeBGLayerSnapshot(Backgrounds[2]),
        MakeBGLayerSnapshot(Backgrounds[3])
    );

    private BackgroundSnapshot.LayerSnapshot MakeBGLayerSnapshot(Background bg)
    {
        bool affine = DisplayControl.BackgroundMode switch
        {
            0 => false,
            1 => bg.ID == 2,
            2 => bg.ID == 2 || bg.ID == 3,
            3 => bg.ID == 2,
            4 => bg.ID == 2,
            5 => bg.ID == 2,
            _ => false
        };

        return new BackgroundSnapshot.LayerSnapshot
        (
            affine,
            bg.Priority,
            bg.CharBaseBlock,
            bg.Mosaic,
            bg.Use256Colors,
            bg.ScreenBaseBlock,
            bg.OverflowWrap,
            bg.ScreenSize,
            bg.XOffset,
            bg.YOffset,
            bg.P[0], bg.P[1], bg.P[2], bg.P[3],
            bg.Origin.X, bg.Origin.Y
        );
    }

    internal unsafe SpriteSnapshot GetSpriteSnapshot()
    {
        byte* oamPtr               = (byte*)_oam.RawPointer;
        ReadOnlySpan<byte> oamData = new(oamPtr, 1024);

        return new SpriteSnapshot
        (
            oamData,
            DisplayControl.Enable[4],
            DisplayControl.ObjVramMapping,
            DisplayControl.BackgroundMode
        );
    }

    internal unsafe PaletteSnapshot GetPaletteSnapshot()
    {
        ushort* pramPtr             = (ushort*)_pram.RawPointer;
        ReadOnlySpan<ushort> colors = new(pramPtr, 512);

        return new PaletteSnapshot(colors);
    }


    internal bool RenderBGToBuffer(int bgIndex, Span<uint> pixels, out int outWidth, out int outHeight)
    {
        outWidth  = 0;
        outHeight = 0;

        byte mode = DisplayControl.BackgroundMode;

        bool active = mode switch
        {
            0           => bgIndex <= 3,
            1           => bgIndex <= 2,
            2           => bgIndex is 2 or 3,
            3 or 4 or 5 => bgIndex == 2,
            _ => false
        };

        if (!active) return false;

        if (mode >= 3)
            return RenderBitmapBGToBuffer(mode, pixels, out outWidth, out outHeight);

        Background bg = Backgrounds[bgIndex];
        bool isAffine = mode switch
        {
            0 => false,
            1 => bgIndex == 2,
            2 => bgIndex >= 2,
            _ => bgIndex == 2
        };

        if (isAffine) return RenderAffineBGToBuffer(bg, pixels, out outWidth, out outHeight);
        else          return RenderTextBGToBuffer(bg, pixels, out outWidth, out outHeight);
    }

    private bool RenderTextBGToBuffer(Background bg, Span<uint> pixels, out int outWidth, out int outHeight)
    {
        var (width, height) = GetTextBGSize(bg.ScreenSize);
        outWidth            = width;
        outHeight           = height;

        if (pixels.Length < width * height) return false;

        uint charBase = bg.CharBaseBlock * 0x4000u;
        bool use256   = bg.Use256Colors;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var (color, transparent) = SampleTextBGTexel((uint)x, (uint)y, bg.ScreenBaseBlock, charBase, use256, width);
                pixels[y * width + x]    = transparent ? 0x00000000 : Framebuffer.ToArgb(color);
            }
        }

        return true;
    }

    private bool RenderAffineBGToBuffer(Background bg, Span<uint> pixels, out int outWidth, out int outHeight)
    {
        var (width, height) = GetAffineBGSize(bg.ScreenSize);
        outWidth            = width;
        outHeight           = height;

        if (pixels.Length < width * height) return false;

        uint charBase   = bg.CharBaseBlock   * 0x4000u;
        uint screenBase = bg.ScreenBaseBlock * 0x800u;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var (color, transparent) = SampleAffineTilemap(x, y, bg, width, height, screenBase, charBase);
                pixels[y * width + x]    = transparent ? 0x00000000 : Framebuffer.ToArgb(color);
            }
        }

        return true;
    }

    private bool RenderBitmapBGToBuffer(byte mode, Span<uint> pixels, out int outWidth, out int outHeight)
    {
        int width, height;
        Func<int, int, (ushort color, bool)> sampler;

        switch (mode)
        {
            case 3:
                width   = 240;
                height  = 160;
                sampler = SampleMode3;
                break;

            case 4:
                width   = 240;
                height  = 160;
                sampler = SampleMode4;
                break;

            case 5:
                width   = 160;
                height  = 128;
                sampler = SampleMode5;
                break;

            default:
                outWidth  = 0;
                outHeight = 0;
                return false;
        }

        outWidth  = width;
        outHeight = height;

        if (pixels.Length < width * height)
            return false;

        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var (color, _)  = sampler(x, y);
                pixels[index++] = Framebuffer.ToArgb(color);
            }
        }

        return true;
    }


    internal bool RenderSpriteToBuffer(int spriteIndex, Span<uint> pixels, out int outWidth, out int outHeight)
    {
        outWidth  = 0;
        outHeight = 0;

        uint oamAddr   = (uint)(spriteIndex * 8);
        ObjAttr0 attr0 = _oam.Fetch<ObjAttr0>(oamAddr + 0);
        ObjAttr1 attr1 = _oam.Fetch<ObjAttr1>(oamAddr + 2);
        ObjAttr2 attr2 = _oam.Fetch<ObjAttr2>(oamAddr + 4);

        if (attr0.ObjMode == 2) return false;

        var (w, h) = SpriteUtility.GetSize(attr0.Shape, attr1.Size);
        outWidth   = w;
        outHeight  = h;

        if (pixels.Length < w * h) return false;

        bool use256     = attr0.Color256;
        uint tileIndex  = attr2.TileIndex;
        uint palette    = attr2.Palette;
        int tilesPerRow = w >> 3;

        bool affine = attr0.Affine;
        bool hFlip  = !affine && attr1.HFlip;
        bool vFlip  = !affine && attr1.VFlip;

        for (int py = 0; py < h; py++)
        {
            int srcY = vFlip ? h - 1 - py : py;

            for (int px = 0; px < w; px++)
            {
                int srcX = hFlip ? w - 1 - px : px;

                if (SampleSpriteTexel(srcX, srcY, tileIndex, tilesPerRow, use256, palette, out ushort color))
                    pixels[py * w + px] = Framebuffer.ToArgb(color);
                else
                    pixels[py * w + px] = 0x00000000;
            }
        }

        return true;
    }
}