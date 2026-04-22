using Trident.Core.Global;
using Trident.Core.Memory.MappedIO;

using static Trident.Core.Global.ArrayExtensions;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    internal sealed class DisplayControlRegister
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

    internal sealed class DisplayStatusRegister(Func<uint> getY)
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

    internal readonly DisplayControlRegister DisplayControl = new();
    internal readonly DisplayStatusRegister  DisplayStatus;


    internal uint Greenswap;

    internal uint VCount;


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

    internal ushort ReadBGxCNT(uint id) => Backgrounds[id].Raw;
    internal void WriteBGxCNT(uint id, ushort value, WriteMask mask)
    {
        Background bg = Backgrounds[id];

        if (mask.IsLower())
        {
            bg.Raw = (ushort)((bg.Raw & 0xFF00) | (byte)value);

            bg.Priority      = (byte)((value >> 0) & 0b11);
            bg.CharBaseBlock = (byte)((value >> 2) & 0b11);
            bg.Mosaic        = ((value >> 6) & 1) != 0;
            bg.Use256Colors  = ((value >> 7) & 1) != 0;
        }

        if (mask.IsUpper())
        {
            bg.Raw = (ushort)((bg.Raw & 0x00FF) | (value & 0xFF00));

            bg.ScreenBaseBlock = (byte)((value >> 8) & 0x1F);
            bg.ScreenSize      = (byte)(value >> 14);

            if (id >= 2) bg.OverflowWrap = ((value >> 13) & 1) != 0;
            else
            {
                bg.OverflowWrap = false;
                bg.Raw          = (ushort)(bg.Raw & ~(1 << 13));
            }
        }
    }

    internal void WriteBGxOFS(uint id, bool vertical, ushort value, WriteMask mask)
    {
        Background bg     = Backgrounds[id];
        ref ushort offset = ref (vertical ? ref bg.YOffset : ref bg.XOffset);

        if (mask.IsLower())
            offset = (ushort)((offset & 0xFF00) | (byte)value);

        if (mask.IsUpper())
            offset = (ushort)((offset & 0x00FF) | ((value & 0x0100) >> 8 << 8));
    }

    internal void WriteBGxP(uint id, AffineParameter p, ushort value, WriteMask mask)
    {
        Background bg   = Backgrounds[id];
        ref short param = ref GetUnsafe(bg.P, (uint)p);

        if (mask.IsLower())
            param = (short)((param & 0xFF00) | (byte)value);

        if (mask.IsUpper())
            param = (short)((param & 0x00FF) | (value & 0xFF00));
    }

    internal void WriteBGxREF(uint id, bool vertical, ushort value, WriteMask wordMask, WriteMask byteMask)
    {
        Background bg = Backgrounds[id];
        ref int param = ref (vertical ? ref bg.Origin.Y : ref bg.Origin.X);

        uint raw = (uint)param;

        uint lo = (uint)(value & 0x00FF);
        uint hi = (uint)(value & 0xFF00);

        switch (wordMask)
        {
            case WriteMask.Lower:
                if (byteMask.IsLower())
                    raw = (raw & ~(0xFFu << 0)) | lo;
                if (byteMask.IsUpper())
                    raw = (raw & ~(0xFFu << 8)) | hi;
                break;

            case WriteMask.Upper:
                if (byteMask.IsLower())
                    raw = (raw & ~(0xFFu << 16)) | (lo << 16);
                if (byteMask.IsUpper())
                {
                    raw  = (raw & ~(0xFFu << 24)) | (hi << 16);
                    raw &= 0x0FFFFFFF;
                    raw  = (uint)raw.ExtendFrom(27);
                }
                break;
        }

        param = (int)raw;
        bg.UpdateReferencePoints();
    }
}


internal enum AffineParameter
{
    A = 0,
    B = 1,
    C = 2,
    D = 3
}