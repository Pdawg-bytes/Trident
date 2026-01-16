namespace Trident.Core.Hardware.Graphics;

public class Framebuffer
{
    public const int Width  = 240;
    public const int Height = 160;

    private readonly uint[] _bufA = new uint[Width * Height];
    private readonly uint[] _bufB = new uint[Width * Height];
    private readonly uint[] _bufC = new uint[Width * Height];

    private uint[] _write;
    private uint[] _read;
    private uint[] _spare;

    private volatile uint[] _latest;
    public uint[] FrontPixels => _latest;

    public Framebuffer()
    {
        _write  = _bufA;
        _read   = _bufB;
        _spare  = _bufC;
        _latest = _read;
    }

    public void SetPixel(uint x, uint y, uint color) => _write[y * Width + x] = color;
    public void Clear(uint color = 0xFF000000) => Array.Fill(_write, color);

    public void Present()
    {
        Interlocked.Exchange(ref _latest, _write);

        var oldRead = _read;
        _read       = _write;
        _write      = _spare;
        _spare      = oldRead;
    }


    internal static uint ToArgb(ushort raw)
    {
        int red   = (raw >> 0)  & 0x1F;
        int green = (raw >> 5)  & 0x1F;
        int blue  = (raw >> 10) & 0x1F;

        red   = (red   << 3) | (red   >> 2);
        green = (green << 3) | (green >> 2);
        blue  = (blue  << 3) | (blue  >> 2);

        return (uint)(0xFF << 24 | red << 16 | green << 8 | blue);
    }
}