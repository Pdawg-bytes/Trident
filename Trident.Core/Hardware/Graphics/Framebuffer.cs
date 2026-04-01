namespace Trident.Core.Hardware.Graphics;

public class Framebuffer
{
    public const int Width  = 240;
    public const int Height = 160;

    private readonly uint[] _write = new uint[Width * Height];
    private readonly object _frontBufferLock = new();

    private readonly uint[] _latest = new uint[Width * Height];
    private int _presentedFrameId;

    public int PresentedFrameId => Volatile.Read(ref _presentedFrameId);

    public Framebuffer()
    {
        Array.Copy(_write, _latest, _latest.Length);
    }


    public void SetPixel(uint x, uint y, uint color) => _write[y * Width + x] = color;
    public void Clear(uint color = 0xFF000000)
    {
        Array.Fill(_write,  color);

        lock (_frontBufferLock)
            Array.Fill(_latest, color);
    }


    public void Present()
    {
        lock (_frontBufferLock)
            Array.Copy(_write, _latest, _latest.Length);

        Interlocked.Increment(ref _presentedFrameId);
    }

    public void CopyFrontPixels(uint[] destination)
    {
        if (destination.Length < _latest.Length)
            throw new ArgumentException("Destination buffer is too small.", nameof(destination));

        lock (_frontBufferLock)
            Array.Copy(_latest, destination, _latest.Length);
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