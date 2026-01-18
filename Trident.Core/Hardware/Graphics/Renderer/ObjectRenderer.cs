using Trident.Core.Global;
using System.Runtime.InteropServices;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    private void RenderObjectLine(uint y)
    {
        if (DisplayControl.Enable[4]) return;

        _objDrawCycles = DisplayControl.HBlankIntervalFree ? 954 : 1210;

        for (uint obj = 0; obj < 128; obj++)
        {

        }
    }


    [StructLayout(LayoutKind.Explicit, Size = 2)]
    readonly struct ObjAttr0
    {
        [FieldOffset(0)] private readonly ushort _raw;

        [FieldOffset(0)] internal readonly byte Y;
        internal uint ObjMode  => (uint)(_raw >> 8)  & 0b11;
        internal uint GfxMode  => (uint)(_raw >> 10) & 0b11;
        internal bool Mosaic   => _raw.IsBitSet(12);
        internal bool Color256 => _raw.IsBitSet(13);
        internal uint Shape    => (uint)(_raw >> 14) & 0b11;
    }

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    readonly struct ObjAttr1
    {
        [FieldOffset(0)] private readonly ushort _raw;

        internal uint X      => (uint)(_raw & 0x01FF);
        internal bool Affine => _raw.IsBitSet(9);
        internal bool HFlip  => _raw.IsBitSet(12);
        internal bool VFlip  => _raw.IsBitSet(13);
        internal uint Size   => (uint)(_raw >> 14) & 0b11;
    }

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    readonly struct ObjAttr2
    {
        [FieldOffset(0)] private readonly ushort _raw;

        internal uint TileIndex => (uint)(_raw & 0x03FF);
        internal uint Priority  => (uint)(_raw >> 10) & 0b11;
        internal uint Palette   => (uint)(_raw >> 12) & 0x0F;
    }
}