using Trident.Core.Memory.MappedIO;
using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    internal DisplayControl DisplayControl = new();
    internal DisplayStatus DisplayStatus   = new();

    internal uint Greenswap;

    internal uint VCount;


    #region Background Registers
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

            // GBATek: BG2/BG3: Display Area Overflow (0=Transparent, 1=Wraparound)
            if (id >= 2) bg.OverflowWrap = ((value >> 13) & 1) != 0;
            else         bg.OverflowWrap = false;
        }
    }

    internal void WriteBGxOFS(uint id, bool vertical, ushort value, WriteMask mask)
    {
        Background bg     = Backgrounds[id];
        ref ushort offset = ref (vertical ? ref bg.YOffset : ref bg.XOffset);

        if (mask.IsLower())
            offset = (ushort)((offset & 0xFF00) | (byte)value);

        if (mask.IsUpper())
            offset = (ushort)((offset & 0x00FF) | ((value & 1) << 8));
    }

    internal void WriteBGxP(uint id, AffineParameter p, ushort value, WriteMask mask)
    {
        Background bg    = Backgrounds[id];
        ref ushort param = ref bg.P[(int)p];

        if (mask.IsLower())
            param = (ushort)((param & 0xFF00) | (byte)value);

        if (mask.IsUpper())
            param = (ushort)((param & 0x00FF) | (value << 8));
    }

    internal void WriteBGxREF(uint id, bool vertical, ushort value, WriteMask wordMask, WriteMask byteMask)
    {
        Background bg   = Backgrounds[id];
        ref uint offset = ref (vertical ? ref bg.YReference : ref bg.XReference);

        uint lo = (uint)(value & 0x00FF);
        uint hi = (uint)(value & 0xFF00);

        switch (wordMask)
        {
            case WriteMask.Lower:
                if (byteMask.IsLower())
                    offset = (offset & ~0x000000FFu) | lo;
                if (byteMask.IsUpper())
                    offset = (offset & ~0x0000FF00u) | hi;
                break;

            case WriteMask.Upper:
                if (byteMask.IsLower())
                    offset = (offset & ~0x00FF0000u) | (lo << 16);
                if (byteMask.IsUpper())
                    offset = (offset & ~0xFF000000u) | (hi << 16);
                break;
        }
    }
    #endregion
}