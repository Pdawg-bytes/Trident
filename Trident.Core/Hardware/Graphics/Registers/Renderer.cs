using Trident.Core.Memory.MappedIO;

namespace Trident.Core.Hardware.Graphics.Registers;

internal class BackgroundControl(int bg)
{
    private readonly int _bg = bg;

    private ushort _bgxcnt;

    internal byte Priority;
    internal byte CharBaseBlock;
    internal byte UnusedBits;
    internal bool Mosaic;
    internal bool Use256Colors;
    internal byte ScreenBaseBlock;
    internal bool OverflowWraparound;
    internal byte ScreenSize;


    internal ushort Read() => _bgxcnt;

    internal void Write(ushort value, WriteMask mask)
    {
        if (mask.IsLower())
        {
            _bgxcnt = (ushort)((_bgxcnt & 0xFF00) | (byte)value);

            Priority      = (byte)((value >> 0) & 0b11);
            CharBaseBlock = (byte)((value >> 2) & 0b11);
            Mosaic        = ((value >> 6) & 1) != 0;
            Use256Colors  = ((value >> 7) & 1) != 0;
        }

        if (mask.IsUpper())
        {
            _bgxcnt = (ushort)((_bgxcnt & 0x00FF) | (value & 0xFF00));

            ScreenBaseBlock = (byte)((value >> 8) & 0x1F);
            ScreenSize      = (byte)(value >> 14);

            // GBATek: BG2/BG3: Display Area Overflow (0=Transparent, 1=Wraparound)
            if (_bg >= 2) OverflowWraparound = ((value >> 13) & 1) != 0;
            else OverflowWraparound = false;
        }
    }
}