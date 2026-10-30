using Trident.Core.Global;
using Trident.Core.Memory.MappedIO;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    internal readonly DisplayControl DisplayControl = new();
    internal readonly DisplayStatus DisplayStatus;

    internal readonly GreenSwap GreenSwap = new();

    internal readonly Mosaic Mosaic      = new();
    internal readonly BlendControl Blend = new();

    internal readonly WindowInside WindowInside;
    internal readonly WindowOutside WindowOutside = new();

    internal uint VCount;
}


internal sealed class DisplayControl
{
    private ushort _dispcnt;

    internal byte BackgroundMode;
    internal uint GetSpriteBoundary() => BackgroundMode >= 3 ? 0x14000u : 0x10000u;

    internal bool CGBMode;
    internal bool FrameSelect;
    internal bool HBlankIntervalFree;
    internal bool ObjVramMapping;
    internal bool ForcedBlank;

    // Display flags: BG0–BG3, OBJ, WIN0, WIN1, OBJWIN
    internal bool[] Enable = new bool[8];


    internal ushort Read() => _dispcnt;

    internal void Write(ushort value, WriteMask mask)
    {
        _dispcnt = MMIO.Merge(_dispcnt, value, mask);

        if (mask.IsLower())
        {
            BackgroundMode     = (byte)(_dispcnt & 0b111);
            CGBMode            = (_dispcnt & (1 << 3)) != 0;
            FrameSelect        = (_dispcnt & (1 << 4)) != 0;
            HBlankIntervalFree = (_dispcnt & (1 << 5)) != 0;
            ObjVramMapping     = (_dispcnt & (1 << 6)) != 0;
            ForcedBlank        = (_dispcnt & (1 << 7)) != 0;
        }

        if (mask.IsUpper())
        {
            for (int i = 0; i < 8; i++)
                Enable[i] = (_dispcnt & (1 << (8 + i))) != 0;
        }
    }
}


internal sealed class DisplayStatus(Func<uint> getY)
{
    private readonly Func<uint> _getY = getY;

    internal bool VBlankFlag;
    internal bool HBlankFlag;

    internal bool VBlankIRQ;
    internal bool HBlankIRQ;
    internal bool VCountIRQ;

    internal byte VCountSetting;


    internal ushort Read() => (ushort)
    (
        (VBlankFlag                 ? 1 : 0) << 0 |
        (HBlankFlag                 ? 1 : 0) << 1 |
        ((_getY() == VCountSetting) ? 1 : 0) << 2 |
        (VBlankIRQ                  ? 1 : 0) << 3 |
        (HBlankIRQ                  ? 1 : 0) << 4 |
        (VCountIRQ                  ? 1 : 0) << 5 |
        VCountSetting                        << 8
    );

    internal void Write(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
        {
            VBlankIRQ = ((value >> 3) & 1) != 0;
            HBlankIRQ = ((value >> 4) & 1) != 0;
            VCountIRQ = ((value >> 5) & 1) != 0;
        }

        if (mask.IsUpper())
            VCountSetting = (byte)(value >> 8);
    }
}


internal sealed class GreenSwap
{
    internal uint Value;

    internal ushort Read() => (ushort)Value;

    internal void Write(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
            Value = value & 1u;
    }

    internal void Reset() => Value = 0;
}


internal enum AffineParameter
{
    A = 0,
    B = 1,
    C = 2,
    D = 3
}


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


    internal ushort ReadControl() => Raw;

    internal void WriteControl(ushort value, WriteMask mask)
    {
        Raw = MMIO.Merge(Raw, value, mask);

        if (mask.IsLower())
        {
            Priority      = (byte)((value >> 0) & 0b11);
            CharBaseBlock = (byte)((value >> 2) & 0b11);
            Mosaic        = ((value >> 6) & 1) != 0;
            Use256Colors  = ((value >> 7) & 1) != 0;
        }

        if (mask.IsUpper())
        {
            ScreenBaseBlock = (byte)((value >> 8) & 0x1F);
            ScreenSize      = (byte)(value >> 14);

            if (ID >= 2) OverflowWrap = ((value >> 13) & 1) != 0;
            else
            {
                OverflowWrap = false;
                Raw          = (ushort)(Raw & ~(1 << 13));
            }
        }
    }

    internal void WriteOffset(bool vertical, ushort value, WriteMask mask)
    {
        ref ushort offset = ref (vertical ? ref YOffset : ref XOffset);

        offset = (ushort)(MMIO.Merge(offset, (ushort)(value & 0x01FF), mask) & 0x01FF);
    }

    internal void WriteAffineParameter(AffineParameter p, ushort value, WriteMask mask)
    {
        ref short param = ref P[(int)p];

        param = (short)MMIO.Merge((ushort)param, value, mask);
    }

    internal void WriteReference(bool vertical, ushort value, WriteMask wordMask, WriteMask byteMask)
    {
        ref int param = ref (vertical ? ref Origin.Y : ref Origin.X);

        uint raw = (uint)param;

        raw = MMIO.Merge(raw, value, wordMask, byteMask);

        if (wordMask == WriteMask.Upper && byteMask.IsUpper())
        {
            raw &= 0x0FFFFFFF;
            raw = (uint)raw.ExtendFrom(27);
        }

        param = (int)raw;
        UpdateReferencePoints();
    }

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


internal sealed class Window(uint id)
{
    internal readonly uint ID = id;

    internal byte Left, Right, Top, Bottom;

    internal bool OBJEnabled;
    internal bool FXEnabled;

    internal byte Layers;
    internal bool BGEnabled(uint bg) => (Layers & (1 << (int)bg)) != 0;


    internal ushort ReadBounds(bool vertical) => vertical
        ? (ushort)((Top  << 8) | Bottom)
        : (ushort)((Left << 8) | Right);

    internal void WriteBounds(bool vertical, ushort value, WriteMask mask)
    {
        if (mask.IsLower())
        {
            if (vertical) Bottom = (byte)value;
            else          Right  = (byte)value;
        }

        if (mask.IsUpper())
        {
            if (vertical) Top  = (byte)(value >> 8);
            else          Left = (byte)(value >> 8);
        }
    }

    internal byte ReadControl() => (byte)
    (
        (Layers & 0x0F)           |
        (OBJEnabled ? 1 : 0) << 4 |
        (FXEnabled  ? 1 : 0) << 5
    );

    internal void WriteControl(byte value)
    {
        Layers     = (byte)(value & 0x0F);
        OBJEnabled = ((value >> 4) & 1) != 0;
        FXEnabled  = ((value >> 5) & 1) != 0;
    }
}


internal sealed class WindowInside(Window[] windows)
{
    private readonly Window[] _windows = windows;

    internal ushort Read() => (ushort)(_windows[0].ReadControl() | (_windows[1].ReadControl() << 8));

    internal void Write(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
            _windows[0].WriteControl((byte)value);

        if (mask.IsUpper())
            _windows[1].WriteControl((byte)(value >> 8));
    }
}


internal sealed class WindowOutside
{
    internal byte OutsideLayers;
    internal bool OutsideOBJEnabled;
    internal bool OutsideFXEnabled;

    internal byte OBJWindowLayers;
    internal bool OBJWindowOBJEnabled;
    internal bool OBJWindowFXEnabled;


    internal ushort Read() => (ushort)
    (
        (OutsideLayers       & 0x0F)        |
        (OutsideOBJEnabled   ? 1 : 0) << 4  |
        (OutsideFXEnabled    ? 1 : 0) << 5  |
        (OBJWindowLayers     & 0x0F)  << 8  |
        (OBJWindowOBJEnabled ? 1 : 0) << 12 |
        (OBJWindowFXEnabled  ? 1 : 0) << 13
    );

    internal void Write(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
        {
            OutsideLayers     = (byte)(value & 0x0F);
            OutsideOBJEnabled = ((value >> 4) & 1) != 0;
            OutsideFXEnabled  = ((value >> 5) & 1) != 0;
        }

        if (mask.IsUpper())
        {
            OBJWindowLayers     = (byte)((value >> 8) & 0x0F);
            OBJWindowOBJEnabled = ((value >> 12) & 1) != 0;
            OBJWindowFXEnabled  = ((value >> 13) & 1) != 0;
        }
    }

    internal void Reset()
    {
        Write(0, WriteMask.Both);
    }
}


internal sealed class Mosaic
{
    internal byte BGHSize = 1;
    internal byte BGVSize = 1;
    internal byte OBJHSize = 1;
    internal byte OBJVSize = 1;

    internal int BGX(bool enabled, int x) => enabled ? Snap(x, BGHSize) : x;
    internal int BGY(bool enabled, int y) => enabled ? Snap(y, BGVSize) : y;

    internal int ObjXOffset(bool enabled, int screenX) => enabled ? screenX - Snap(screenX, OBJHSize) : 0;
    internal int ObjYOffset(bool enabled, int y)       => enabled ? y       - Snap(y,       OBJVSize) : 0;

    private static int Snap(int coordinate, byte size) => coordinate - coordinate % (size + 1);

    internal void Write(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
        {
            BGHSize = (byte)(value        & 0xF);
            BGVSize = (byte)((value >> 4) & 0xF);
        }

        if (mask.IsUpper())
        {
            OBJHSize = (byte)((value >>  8) & 0xF);
            OBJVSize = (byte)((value >> 12) & 0xF);
        }
    }

    internal void Reset() => Write(0, WriteMask.Both);
}


internal enum BlendMode
{
    None     = 0,
    Alpha    = 1,
    Brighten = 2,
    Darken   = 3
}


internal sealed class BlendControl
{
    private ushort _bldcnt;

    internal readonly bool[] FirstTarget  = new bool[6];
    internal readonly bool[] SecondTarget = new bool[6];

    internal BlendMode Mode;

    internal byte EVA;
    internal byte EVB;
    internal byte EVY;

    internal ushort ReadControl() => _bldcnt;

    internal void WriteControl(ushort value, WriteMask mask)
    {
        _bldcnt = (ushort)(MMIO.Merge(_bldcnt, (ushort)(value & 0x3FFF), mask) & 0x3FFF);

        if (mask.IsLower())
        {
            for (int i = 0; i < 6; i++)
                FirstTarget[i] = (_bldcnt & (1 << i)) != 0;

            Mode = (BlendMode)((_bldcnt >> 6) & 0b11);
        }

        if (mask.IsUpper())
        {
            for (int i = 0; i < 6; i++)
                SecondTarget[i] = (_bldcnt & (1 << (8 + i))) != 0;
        }
    }

    internal void WriteAlpha(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
            EVA = (byte)Math.Min(value & 0x1F, 16);

        if (mask.IsUpper())
            EVB = (byte)Math.Min((value >> 8) & 0x1F, 16);
    }

    internal void WriteBrightness(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
            EVY = (byte)Math.Min(value & 0x1F, 16);
    }

    internal void Reset()
    {
        WriteControl(0, WriteMask.Both);
        EVA = EVB = EVY = 0;
    }
}