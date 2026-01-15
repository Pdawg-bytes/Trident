using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private void RenderMode3BG()
    {
        uint rowBase = VCount * 240 << 1;

        for (uint x = 0; x < 240; x++)
        {
            ushort color = _vram.Fetch<ushort>(rowBase + (x << 1));
            _framebuffer.SetPixel(x, VCount, Framebuffer.ToArgb(color));
        }
    }

    private void RenderMode4BG()
    {
        uint baseFrame = DisplayControl.FrameSelect ? 0xA000u : 0x0000u;
        uint rowBase   = baseFrame + VCount * 240;

        for (uint x = 0; x < 240; x++)
        {
            uint index   = (uint)(_vram.Fetch<byte>(rowBase + x) << 1);
            ushort color = _pram.Fetch<ushort>(index);
            _framebuffer.SetPixel(x, VCount, Framebuffer.ToArgb(color));
        }
    }

    private void RenderMode5BG()
    {
        uint baseFrame = DisplayControl.FrameSelect ? 0xA000u : 0x0000u;
        uint rowBase = baseFrame + VCount * 320;

        // TODO: clear rest of BG2 line, leave to compositor to fill with transparent
        for (uint x = 0; x < 160; x++)
        {
            ushort color = _vram.Fetch<ushort>(rowBase + (x << 1));
            _framebuffer.SetPixel(x, VCount, Framebuffer.ToArgb(color));
        }
    }
}