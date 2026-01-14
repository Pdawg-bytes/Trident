using Trident.Core.Memory.MappedIO;

namespace Trident.Core.Hardware.Graphics.Registers;

internal class DisplayControl
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
        if (mask.IsLower())
        {
            _dispcnt = (ushort)((_dispcnt & 0xFF00) | (byte)value);

            BackgroundMode     = (byte)(_dispcnt & 0b111);
            CGBMode            = (_dispcnt & (1 << 3)) != 0;
            FrameSelect        = (_dispcnt & (1 << 4)) != 0;
            HBlankIntervalFree = (_dispcnt & (1 << 5)) != 0;
            ObjVramMapping     = (_dispcnt & (1 << 6)) != 0;
            ForcedBlank        = (_dispcnt & (1 << 7)) != 0;
        }

        if (mask.IsUpper())
        {
            _dispcnt = (ushort)((_dispcnt & 0x00FF) | (value & 0xFF00));

            for (int i = 0; i < 8; i++)
                Enable[i] = (_dispcnt & (1 << (8 + i))) != 0;
        }
    }
}


internal class DisplayStatus
{
    internal bool VBlankFlag;
    internal bool HBlankFlag;
    internal bool VCountFlag;

    internal bool VBlankIRQ;
    internal bool HBlankIRQ;
    internal bool VCountIRQ;

    internal byte VCountSetting;


    internal ushort Read() => (ushort)
    (
        (VBlankFlag ? 1 : 0) << 0 |
        (HBlankFlag ? 1 : 0) << 1 |
        (VCountFlag ? 1 : 0) << 2 |
        (VBlankIRQ  ? 1 : 0) << 3 |
        (HBlankIRQ  ? 1 : 0) << 4 |
        (VCountIRQ  ? 1 : 0) << 5 |
        VCountSetting << 8
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