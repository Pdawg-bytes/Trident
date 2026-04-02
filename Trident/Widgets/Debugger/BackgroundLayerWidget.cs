using ImGuiNET;
using System.Numerics;
using Trident.Utilities;
using OpenTK.Graphics.OpenGL4;
using Trident.Core.Debugging.Snapshots;

using static Trident.Widgets.WidgetHelpers;

namespace Trident.Widgets.Debugger;

internal delegate bool BGRenderDelegate(int bgIndex, Span<uint> pixels, out int width, out int height);

internal class BackgroundLayerWidget(ImFontPtr monoFont, Func<BackgroundSnapshot> getSnapshot, BGRenderDelegate renderBG) : IWidget
{
    private readonly Func<BackgroundSnapshot> _getSnapshot = getSnapshot;
    private readonly BGRenderDelegate _renderBG            = renderBG;
    private readonly ImFontPtr _monoFont                   = monoFont;

    private const int MaxBGPixels = 1024 * 1024;

    private readonly uint[] _pixelBuffer = new uint[MaxBGPixels];
    private int _bgTexture               = 0;
    private int _texWidth                = 0;
    private int _texHeight               = 0;

    private readonly string[] TabNames = ["BG0", "BG1", "BG2", "BG3"];

    private readonly string[] ModeDescriptions =
    [
        "Mode 0: 4x Text",
        "Mode 1: 2x Text + 1x Affine",
        "Mode 2: 2x Affine",
        "Mode 3: Bitmap 16bpp",
        "Mode 4: Bitmap 8bpp (Paged)",
        "Mode 5: Bitmap 16bpp (Small, Paged)"
    ];

    private readonly string[][] ScreenSizeLabels =
    [
        ["256x256", "512x256", "256x512", "512x512"],
        ["128x128", "256x256", "512x512", "1024x1024"]
    ];


    public bool IsVisible { get; set; } = true;

    public string Name  => "Background Layers";
    public string Group => "PPU";

    public void Render()
    {
        if (!IsVisible) return;

        if (!ImGui.Begin("Background Layers"))
        {
            ImGui.End();
            return;
        }

        BackgroundSnapshot snapshot = _getSnapshot();

        Span<char> numBuf  = stackalloc char[32];
        StackString numStr = new(numBuf);

        byte mode = snapshot.BackgroundMode;

        Span<char> modeLine = stackalloc char[64];
        StackString modeStr = new(modeLine);

        if (mode < 6) modeStr.Append(ModeDescriptions[mode]);
        else          modeStr.Append("Invalid Mode");

        if (snapshot.ForcedBlank)
            modeStr.Append(", Forced Blank");

        ImGui.TextUnformatted(modeStr.AsSpan());

        ImGui.Separator();

        Span<BackgroundSnapshot.LayerSnapshot> layers =
        [
            snapshot.BG0,
            snapshot.BG1,
            snapshot.BG2,
            snapshot.BG3
        ];

        Span<bool> enabled =
        [
            snapshot.BG0Enabled,
            snapshot.BG1Enabled,
            snapshot.BG2Enabled,
            snapshot.BG3Enabled
        ];

        if (ImGui.BeginTabBar("##bgtabs"))
        {
            for (int i = 0; i < 4; i++)
            {
                if (ImGui.BeginTabItem(TabNames[i]))
                {
                    RenderBGTab(i, layers[i], enabled[i], mode, numBuf, ref numStr);
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }

        ImGui.End();
    }


    private void RenderBGTab(int i, BackgroundSnapshot.LayerSnapshot bg, bool isEnabled, byte mode, Span<char> numBuf, ref StackString numStr)
    {
        int flagCols = bg.Affine ? 3 : 2;
        if (ImGui.BeginTable("##bgflags", flagCols, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableNextRow();
            ImGui.PushFont(_monoFont);
            ImGui.TableSetColumnIndex(0); RenderFlagCell("Enabled", isEnabled);
            ImGui.TableSetColumnIndex(1); RenderFlagCell("Mosaic", bg.Mosaic);
            if (bg.Affine) 
            { 
                ImGui.TableSetColumnIndex(2); 
                RenderFlagCell("Wrap", bg.OverflowWrap); 
            }
            ImGui.PopFont();
            ImGui.EndTable();
        }

        if (ImGui.BeginTable("##bgprops", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Property");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            ImGui.PushFont(_monoFont);

            RenderPropertyRowText("Type", bg.Affine ? "Affine" : "Text");

            RenderPropertyRow("Priority", numBuf, ref numStr, bg.Priority);

            numStr = StackString.Interpolate(numBuf, $"0x{(uint)(bg.CharBaseBlock * 0x4000):X8}");
            RenderPropertyRowText("Character Base", numStr.AsSpan());

            numStr = StackString.Interpolate(numBuf, $"0x{(uint)(bg.ScreenBaseBlock * 0x800):X8}");
            RenderPropertyRowText("Screen Base", numStr.AsSpan());

            RenderPropertyRowText("Color Mode", bg.Use256Colors ? "256 colors" : "16 colors");

            int sizeTableIndex = bg.Affine ? 1 : 0;
            ReadOnlySpan<char> sizeLabel = ScreenSizeLabels[sizeTableIndex][bg.ScreenSize];
            RenderPropertyRowText("Screen Size", sizeLabel);

            if (bg.Affine)
            {
                numStr = StackString.Interpolate(numBuf, $"({bg.ReferenceX / 256f:F2}, {bg.ReferenceY / 256f:F2})");
                RenderPropertyRowText("Reference", numStr.AsSpan());

                numStr = StackString.Interpolate(numBuf, $"{bg.PA / 256f:F4}");
                RenderPropertyRowText("PA", numStr.AsSpan());
                numStr = StackString.Interpolate(numBuf, $"{bg.PB / 256f:F4}");
                RenderPropertyRowText("PB", numStr.AsSpan());
                numStr = StackString.Interpolate(numBuf, $"{bg.PC / 256f:F4}");
                RenderPropertyRowText("PC", numStr.AsSpan());
                numStr = StackString.Interpolate(numBuf, $"{bg.PD / 256f:F4}");
                RenderPropertyRowText("PD", numStr.AsSpan());
            }
            else
            {
                numStr = StackString.Interpolate(numBuf, $"({bg.XOffset}, {bg.YOffset})");
                RenderPropertyRowText("Offset", numStr.AsSpan());
            }

            ImGui.PopFont();
            ImGui.EndTable();
        }

        RenderBGPreview(i);
    }

    private void RenderBGPreview(int bgIndex)
    {
        if (_renderBG(bgIndex, _pixelBuffer, out int w, out int h) && w > 0 && h > 0)
        {
            EnsureTexture(w, h);

            GL.BindTexture(TextureTarget.Texture2D, _bgTexture);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, w, h,
                PixelFormat.Bgra, PixelType.UnsignedByte, _pixelBuffer);

            float availWidth = ImGui.GetContentRegionAvail().X;
            float scale      = MathF.Min(1f, availWidth / w);
            ImGui.Image(_bgTexture, new Vector2(w * scale, h * scale));
        }
        else
        {
            ImGui.TextDisabled("Not active in current mode");
        }
    }

    private void EnsureTexture(int width, int height)
    {
        if (_bgTexture == 0)
        {
            _bgTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _bgTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }

        if (_texWidth != width || _texHeight != height)
        {
            GL.BindTexture(TextureTarget.Texture2D, _bgTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, nint.Zero);

            _texWidth  = width;
            _texHeight = height;
        }
    }
}