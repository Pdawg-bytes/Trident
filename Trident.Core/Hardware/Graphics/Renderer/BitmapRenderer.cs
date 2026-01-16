using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private void RenderMode3BG(uint y)
    {
        if (!DisplayControl.Enable[2]) return;

        Background bg     = Backgrounds[2];
        LayerPixel[] line = _bgLines[2];

        uint rowBase = y * 240 << 1;

        for (uint x = 0; x < 240; x++)
        {
            ushort color = _vram.Fetch<ushort>(rowBase + (x << 1));

            line[x] = new()
            {
                Color       = color,
                Priority    = bg.Priority,
                Transparent = false,
                Source      = (byte)bg.ID
            };
        }
    }

    private void RenderMode4BG(uint y)
    {
        if (!DisplayControl.Enable[2]) return;

        Background bg     = Backgrounds[2];
        LayerPixel[] line = _bgLines[2];

        uint baseFrame = DisplayControl.FrameSelect ? 0xA000u : 0x0000u;
        uint rowBase   = baseFrame + y * 240;

        for (uint x = 0; x < 240; x++)
        {
            uint index   = (uint)(_vram.Fetch<byte>(rowBase + x) << 1);
            ushort color = _pram.Fetch<ushort>(index);

            line[x] = new()
            {
                Color       = color,
                Priority    = bg.Priority,
                Transparent = false,
                Source      = (byte)bg.ID
            };
        }
    }

    private void RenderMode5BG(uint y)
    {
        if (!DisplayControl.Enable[2]) return;

        Background bg     = Backgrounds[2];
        LayerPixel[] line = _bgLines[2];

        uint baseFrame = DisplayControl.FrameSelect ? 0xA000u : 0x0000u;
        uint rowBase   = baseFrame + y * 320;

        for (uint x = 0; x < ScreenWidth; x++)
        {
            ushort color = (x < 160 && y < 128) 
                ? _vram.Fetch<ushort>(rowBase + (x << 1))
                : _pram.Fetch<ushort>(0);

            line[x] = new()
            {
                Color       = color,
                Priority    = bg.Priority,
                Transparent = false,
                Source      = (byte)bg.ID
            };
        }
    }
}