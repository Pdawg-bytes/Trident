using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private void RenderTextBG(uint id)
    {
        if (!DisplayControl.Enable[id]) return;

        Background bg = Backgrounds[id];

        for (uint x = 0; x < ScreenWidth; x++)
        {
        }
    }
}