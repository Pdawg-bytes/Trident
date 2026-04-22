namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private (ushort color, bool transparent) SampleMode3(int texX, int texY)
    {
        uint addr    = (uint)(texY * 240 + texX) << 1;
        ushort color = _vram.Fetch<ushort>(addr);

        return (color, false);
    }

    private (ushort color, bool transparent) SampleMode4(int texX, int texY)
    {
        uint baseFrame = DisplayControl.FrameSelect ? 0xA000u : 0x0000u;
        uint index     = _vram.Fetch<byte>(baseFrame + (uint)(texY * 240 + texX));
        ushort color   = _pram.Fetch<ushort>(index << 1);

        return (color, false);
    }

    private (ushort color, bool transparent) SampleMode5(int texX, int texY)
    {
        if ((uint)texX >= 160 || (uint)texY >= 128)
            return (_pram.Fetch<ushort>(0), false);

        uint baseFrame = DisplayControl.FrameSelect ? 0xA000u : 0x0000u;
        uint addr      = baseFrame + ((uint)(texY * 160 + texX) << 1);

        ushort color = _vram.Fetch<ushort>(addr);
        return (color, false);
    }
}