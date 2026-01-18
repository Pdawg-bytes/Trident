namespace Trident.Core.Hardware.Graphics.Registers;

internal sealed class Background(uint bg)
{
    internal struct ReferencePoint
    {
        internal int X;
        internal int Y;
    }

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

    internal readonly short[] P = new short[4];
    internal ReferencePoint Origin;
    internal ReferencePoint Lerp;

    internal void UpdateReferencePoints() => Lerp = Origin;

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

        P[0] = 0x0100;
        P[1] = 0x0000;
        P[2] = 0x0000;
        P[3] = 0x0100;

        Origin = new();
        Lerp   = new();
    }
}

internal enum AffineParameter
{
    A = 0,
    B = 1,
    C = 2,
    D = 3
}