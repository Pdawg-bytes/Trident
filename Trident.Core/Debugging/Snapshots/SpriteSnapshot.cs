using Trident.Core.Hardware.Graphics;
using System.Runtime.CompilerServices;

namespace Trident.Core.Debugging.Snapshots;

public unsafe struct SpriteSnapshot
{
    private fixed byte _oamData[1024];

    public readonly bool ObjEnabled;
    public readonly bool ObjVramMapping;
    public readonly byte BackgroundMode;

    public SpriteSnapshot(ReadOnlySpan<byte> oamData, bool objEnabled, bool objVramMapping, byte bgMode)
    {
        ObjEnabled     = objEnabled;
        ObjVramMapping = objVramMapping;
        BackgroundMode = bgMode;

        for (int i = 0; i < 1024; i++)
            _oamData[i] = oamData[i];
    }


    public readonly ushort GetAttr(int spriteIndex, int attrIndex)
    {
        int offset = spriteIndex * 8 + attrIndex * 2;
        fixed (byte* ptr = _oamData)
            return *(ushort*)(ptr + offset);
    }


    public readonly struct SpriteEntry
    (
        int index, short x, short y,
        short width, short height,
        byte shape, byte size,
        byte objMode, byte gfxMode,
        bool affine, bool doubleSize, bool mosaic, bool color256,
        bool hFlip, bool vFlip,
        byte affineIndex,
        ushort tileIndex, byte priority, byte palette
    )
    {
        public readonly int Index        = index;
        public readonly short X          = x;
        public readonly short Y          = y;
        public readonly short Width      = width;
        public readonly short Height     = height;
        public readonly byte Shape       = shape;
        public readonly byte Size        = size;
        public readonly byte ObjMode     = objMode;
        public readonly byte GfxMode     = gfxMode;
        public readonly bool Affine      = affine;
        public readonly bool DoubleSize  = doubleSize;
        public readonly bool Mosaic      = mosaic;
        public readonly bool Color256    = color256;
        public readonly bool HFlip       = hFlip;
        public readonly bool VFlip       = vFlip;
        public readonly byte AffineIndex = affineIndex;
        public readonly ushort TileIndex = tileIndex;
        public readonly byte Priority    = priority;
        public readonly byte Palette     = palette;

        public readonly bool IsVisible => ObjMode != 2;
    }

    public readonly SpriteEntry DecodeSprite(int index)
    {
        ushort raw0 = GetAttr(index, 0);
        ushort raw1 = GetAttr(index, 1);
        ushort raw2 = GetAttr(index, 2);

        ObjAttr0 attr0 = Unsafe.As<ushort, ObjAttr0>(ref raw0);
        ObjAttr1 attr1 = Unsafe.As<ushort, ObjAttr1>(ref raw1);
        ObjAttr2 attr2 = Unsafe.As<ushort, ObjAttr2>(ref raw2);

        var (width, height) = SpriteUtility.GetSize(attr0.Shape, attr1.Size);

        int rawY = attr0.Y;
        int rawX = (int)attr1.X;
        short y  = (short)(rawY >= 160 ? rawY - 256 : rawY);
        short x  = (short)(rawX >= 240 ? rawX - 512 : rawX);

        return new SpriteEntry
        (
            index:       index,
            x:           x, 
            y:           y,
            width:       width, 
            height:      height,
            shape:       (byte)attr0.Shape, 
            size:        (byte)attr1.Size,
            objMode:     (byte)attr0.ObjMode,
            gfxMode:     (byte)attr0.GfxMode,
            affine:      attr0.Affine,
            doubleSize:  attr0.DoubleSize,
            mosaic:      attr0.Mosaic,
            color256:    attr0.Color256,
            hFlip:       !attr0.Affine && attr1.HFlip,
            vFlip:       !attr0.Affine && attr1.VFlip,
            affineIndex: attr0.Affine ? (byte)attr1.AffineIndex : (byte)0,
            tileIndex:   (ushort)attr2.TileIndex,
            priority:    attr2.Priority,
            palette:     (byte)attr2.Palette
        );
    }


    public readonly (short PA, short PB, short PC, short PD) GetAffineParams(int groupIndex)
    {
        int baseOffset = groupIndex * 32;
        fixed (byte* ptr = _oamData)
        {
            short pa = *(short*)(ptr + baseOffset + 0x06);
            short pb = *(short*)(ptr + baseOffset + 0x0E);
            short pc = *(short*)(ptr + baseOffset + 0x16);
            short pd = *(short*)(ptr + baseOffset + 0x1E);
            return (pa, pb, pc, pd);
        }
    }
}