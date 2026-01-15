using System.Runtime.CompilerServices;

namespace Trident.Core.Hardware.Graphics.Registers;

internal class Background(uint bg)
{
    [InlineArray(4)]
    internal struct AffineParameters { ushort _e0; }

    internal readonly uint ID = bg;

    internal ushort Raw;

    internal byte Priority;
    internal byte CharBaseBlock;
    internal bool Mosaic;
    internal bool Use256Colors;
    internal byte ScreenBaseBlock;
    internal bool OverflowWrap;
    internal byte ScreenSize;

    internal ushort XOffset;
    internal ushort YOffset;

    internal AffineParameters P = new();
    internal uint XReference;
    internal uint YReference;


    internal void Reset()
    {
        Raw             = 0;
        Priority        = 0;
        CharBaseBlock   = 0;
        Mosaic          = false;
        Use256Colors    = false;
        ScreenBaseBlock = 0;
        OverflowWrap    = false;
        ScreenSize      = 0;

        P          = new();
        XReference = 0;
        YReference = 0;
    }
}

internal enum AffineParameter
{
    A = 0,
    B = 1,
    C = 2,
    D = 3
}