namespace Trident.Core.Debugging.Snapshots;

public unsafe struct PaletteSnapshot
{
    private fixed ushort _colors[512];

    public PaletteSnapshot(ReadOnlySpan<ushort> colors)
    {
        for (int i = 0; i < 512; i++)
            _colors[i] = colors[i];
    }


    public readonly ushort GetColor(int index)
    {
        fixed (ushort* ptr = _colors)
            return ptr[index];
    }

    public readonly ReadOnlySpan<ushort> Colors
    {
        get
        {
            fixed (ushort* ptr = _colors)
                return new ReadOnlySpan<ushort>(ptr, 512);
        }
    }
}