using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    internal DisplayControl DisplayControl = new();
    internal DisplayStatus DisplayStatus = new();

    internal uint Greenswap;

    internal uint VCount;

    internal BackgroundControl[] BackgroundControls =
    [
        new(0),
        new(1),
        new(2),
        new(3)
    ];
}