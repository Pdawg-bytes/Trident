namespace Trident.Core.Hardware.Graphics
{
    public class Framebuffer
    {
        public const int Width = 240;
        public const int Height = 160;

        public readonly uint[] Pixels = new uint[Width * Height];


        internal void SetPixel(int x, int y, uint color) => Pixels[y * Width + x] = color;

        internal void Clear(uint color = 0xFF000000) => Array.Fill(Pixels, color);

        internal static uint ToArgb(ushort raw)
        {
            int red   = (raw >> 0)  & 0x1F;
            int green = (raw >> 5)  & 0x1F;
            int blue  = (raw >> 10) & 0x1F;

            red   = (red << 3)   | (red >> 2);
            green = (green << 3) | (green >> 2);
            blue  = (blue << 3)  | (blue >> 2);

            return (uint)(0xFF << 24 | red << 16 | green << 8 | blue);
        }
    }
}