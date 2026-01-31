using Trident.Core.Global;
using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private struct LayerPixel
    {
        internal ushort Color;
        internal bool   Transparent;
        internal byte   Priority;
        internal byte   Source;
        internal uint   Generation;
    }

    private readonly uint[][] ActiveBGs =
    [
        [ 0, 1, 2, 3 ],
        [ 0, 1, 2 ],
        [ 2, 3 ],
        [ 2 ],
        [ 2 ],
        [ 2 ],
    ];

    private readonly LayerPixel DefaultPixel = new()
    {
        Color       = 0,
        Transparent = true,
        Priority    = 0xFF,
        Source      = 0xFF
    };


    private void CompositeScanline(uint y, byte mode)
    {
        uint[] active = ActiveBGs[mode];

        for (uint x = 0; x < ScreenWidth; x++)
        {
            LayerPixel best  = DefaultPixel;
            LayerPixel objPx = _objLine[x];

            if (objPx.Generation == _pixelGeneration && !objPx.Transparent)
                best = objPx;

            foreach (uint bgId in active)
            {
                LayerPixel px = _bgLines[bgId][x];

                if (px.Generation != _pixelGeneration)
                    continue;

                if (!px.Transparent)
                {
                    if (best.Transparent)
                        best = px;
                    else if (px.Priority < best.Priority)
                        best = px;
                }
            }

            _framebuffer.SetPixel(x, y, Framebuffer.ToArgb(best.Color));
        }
    }


    private void ResetScanlineBuffers()
    {
        for (int bg = 0; bg < 4; bg++)
        {
            LayerPixel[] line = _bgLines[bg];

            for (int x = 0; x < ScreenWidth; x++)
                line[x] = DefaultPixel;
        }

        for (int x = 0; x < ScreenWidth; x++)
            _objLine[x] = DefaultPixel;
    }
}