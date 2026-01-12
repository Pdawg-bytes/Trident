using ImGuiNET;
using System.Numerics;

namespace Trident.Utilities;

internal static class Color
{
    internal static readonly uint HiglightBackground = ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark]) & 0x2FFFFFFF;

    internal static Vector4 MakeHighlight(Vector4 accent)
    {
        var (h, s, v) = ColorToHSV(accent);

        s = 0.15f;
        v = 0.95f;

        return HSVToColor(h, s, v);
    }

    private static (float h, float s, float v) ColorToHSV(Vector4 color)
    {
        float h;
        float s;
        float v;

        float r = color.X;
        float g = color.Y;
        float b = color.Z;

        float max = MathF.Max(r, MathF.Max(g, b));
        float min = MathF.Min(r, MathF.Min(g, b));
        float delta = max - min;

        v = max;

        s = max == 0f ? 0f : delta / max;

        if (delta == 0f)
            h = 0f;
        else if (max == r)
            h = ((g - b) / delta) % 6f;
        else if (max == g)
            h = ((b - r) / delta) + 2f;
        else
            h = ((r - g) / delta) + 4f;

        h /= 6f;
        if (h < 0f) h += 1f;

        return (h, s, v);
    }

    private static Vector4 HSVToColor(float h, float s, float v)
    {
        float r = v, g = v, b = v;

        if (s != 0f)
        {
            h = (h % 1f) * 6f;
            int sector = (int)MathF.Floor(h);
            float fraction = h - sector;

            float p = v * (1f - s);
            float q = v * (1f - s * fraction);
            float t = v * (1f - s * (1f - fraction));

            switch (sector)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                case 5: r = v; g = p; b = q; break;
            }
        }

        return new Vector4(r, g, b, 1f);
    }
}