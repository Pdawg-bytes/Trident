using System.Runtime.CompilerServices;
using Trident.Core.Hardware.Graphics.Registers;

using static Trident.Core.Global.ArrayExtensions;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private void RenderAffineBG<TSampler>(uint id, uint y)
        where TSampler : struct, ISampler
    {
        if (!DisplayControl.Enable[id])
            return;

        Background bg     = Backgrounds[id];
        LayerPixel[] line = _bgLines[id];

        var (width, height) = GetAffineBGSize(bg.ScreenSize);

        uint charBase   = bg.CharBaseBlock   * 0x4000u;
        uint screenBase = bg.ScreenBaseBlock * 0x800u;

        byte priority = bg.Priority;
        byte source   = (byte)bg.ID;

        int xRef = bg.Lerp.X;
        int yRef = bg.Lerp.Y;

        short pa = bg.P[0];
        short pb = bg.P[1];
        short pc = bg.P[2];
        short pd = bg.P[3];

        for (uint x = 0; x < ScreenWidth; x++)
        {
            int texX = xRef >> 8;
            int texY = yRef >> 8;

            (ushort color, bool transparent) = TSampler.Mode switch
            {
                0 => SampleAffineTilemap(texX, texY, bg, width, height, screenBase, charBase),
                3 => SampleMode3(texX, texY),
                4 => SampleMode4(texX, texY),
                5 => SampleMode5(texX, texY),
                _ => throw new Exception("Invalid affine sampler")
            };

            ref LayerPixel px = ref GetUnsafe(line, x);
            px.Color       = color;
            px.Transparent = transparent;
            px.Priority    = priority;
            px.Source      = source;
            px.Generation  = _pixelGeneration;

            xRef += pa;
            yRef += pc;
        }

        bg.Lerp.X += pb; 
        bg.Lerp.Y += pd;
    }

    private (ushort color, bool transparent) SampleAffineTilemap(
        int texX, int texY, 
        Background bg, ushort width, ushort height,
        uint screenBase, uint charBase)
    {
        bool inBounds = (uint)texX < width && (uint)texY < height;

        if (!inBounds)
        {
            if (!bg.OverflowWrap)
                return (0, true);

            texX &= width  - 1;
            texY &= height - 1;
        }

        int tileX = texX >> 3;
        int tileY = texY >> 3;

        uint mapIndex = (uint)(tileY * (width >> 3) + tileX);
        byte tileId   = _vram.Fetch<byte>(screenBase + mapIndex);

        int px = texX & 7;
        int py = texY & 7;

        return SampleTile8bpp(charBase, tileId, px, py, 0u);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (ushort width, ushort height) GetAffineBGSize(byte screenSize) => screenSize switch
    {
        0 => (128,  128),
        1 => (256,  256),
        2 => (512,  512),
        3 => (1024, 1024),
        _ => (0, 0)
    };
}


internal interface ISampler { static abstract uint Mode { get; } }

internal struct TileSampler    : ISampler { public static uint Mode => 0; }
internal struct Bitmap3Sampler : ISampler { public static uint Mode => 3; }
internal struct Bitmap4Sampler : ISampler { public static uint Mode => 4; }
internal struct Bitmap5Sampler : ISampler { public static uint Mode => 5; }