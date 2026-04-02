using ImGuiNET;
using System.Numerics;
using Trident.Utilities;
using OpenTK.Graphics.OpenGL4;
using Trident.Core.Debugging.Snapshots;

using static Trident.Widgets.WidgetHelpers;

namespace Trident.Widgets.Debugger;

internal delegate bool SpriteRenderDelegate(int spriteIndex, Span<uint> pixels, out int width, out int height);

internal class SpriteViewerWidget(ImFontPtr monoFont, Func<SpriteSnapshot> getSnapshot, SpriteRenderDelegate renderSprite) : IWidget
{
    private readonly Func<SpriteSnapshot> _getSnapshot  = getSnapshot;
    private readonly SpriteRenderDelegate _renderSprite = renderSprite;
    private readonly ImFontPtr _monoFont                = monoFont;

    private const int SpritesPerPage  = 16;
    private const int TotalSprites    = 128;
    private const int TotalPages      = TotalSprites / SpritesPerPage;
    private const int MaxSpritePixels = 64 * 64;

    private int _currentPage   = 0;
    private bool _hideDisabled = false;

    private readonly uint[] _pixelBuffer   = new uint[MaxSpritePixels];
    private readonly int[] _spriteTextures = new int[SpritesPerPage];
    private readonly int[] _texWidths      = new int[SpritesPerPage];
    private readonly int[] _texHeights     = new int[SpritesPerPage];
    private int _visibleSlot;

    private readonly string[] ShapeNames   = ["Square", "Wide", "Tall", "Invalid"];
    private readonly string[] GfxModeNames = ["Normal", "Blend", "Window", "Invalid"];


    public bool IsVisible { get; set; } = true;

    public string Name  => "Sprite Viewer";
    public string Group => "PPU";

    public void Render()
    {
        if (!IsVisible) return;

        if (!ImGui.Begin("Sprite Viewer"))
        {
            ImGui.End();
            return;
        }

        SpriteSnapshot snapshot = _getSnapshot();

        if (ImGui.BeginTable("##objstatus", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableNextRow();
            ImGui.PushFont(_monoFont);
            ImGui.TableSetColumnIndex(0); RenderFlagCell("Enabled", snapshot.ObjEnabled);
            ImGui.TableSetColumnIndex(1); RenderFlagCell("1D", snapshot.ObjVramMapping);

            ImGui.PopFont();
            ImGui.EndTable();
        }

        if (ImGui.ArrowButton("##prev", ImGuiDir.Left) && _currentPage > 0)
            _currentPage--;

        ImGui.SameLine();

        Span<char> pageBuf  = stackalloc char[16];
        StackString pageStr = StackString.Interpolate(pageBuf, $"Page {_currentPage + 1}/{TotalPages}");
        ImGui.TextUnformatted(pageStr.AsSpan());

        ImGui.SameLine();

        if (ImGui.ArrowButton("##next", ImGuiDir.Right) && _currentPage < TotalPages - 1)
            _currentPage++;

        ImGui.SameLine();
        ImGui.Checkbox("Hide disabled", ref _hideDisabled);

        ImGui.Separator();

        int startIndex = _currentPage * SpritesPerPage;
        int endIndex   = startIndex + SpritesPerPage;

        Span<char> numBuf  = stackalloc char[32];
        StackString numStr = new(numBuf);

        Span<char> headerBuf = stackalloc char[16];

        _visibleSlot = 0;

        for (int i = startIndex; i < endIndex; i++)
        {
            SpriteSnapshot.SpriteEntry sprite = snapshot.DecodeSprite(i);

            bool isDisabled = sprite.ObjMode == 2;
            if (_hideDisabled && isDisabled)
                continue;

            ImGui.PushID(i);

            StackString headerStr = StackString.Interpolate(headerBuf, $"OBJ {i:D3}");

            if (isDisabled) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));

            if (ImGui.CollapsingHeader(headerStr.AsSpan()))
            {
                RenderSpritePreview(i, ref sprite);

                int flagCols = sprite.Affine ? 3 : 4;
                if (ImGui.BeginTable("##sprflags", flagCols, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableNextRow();
                    ImGui.PushFont(_monoFont);

                    int col = 0;

                    if (!sprite.Affine)
                    {
                        ImGui.TableSetColumnIndex(col++); RenderFlagCell("H-Flip", sprite.HFlip);
                        ImGui.TableSetColumnIndex(col++); RenderFlagCell("V-Flip", sprite.VFlip);
                    }
                    else
                    {
                        ImGui.TableSetColumnIndex(col++); RenderFlagCell("Double", sprite.DoubleSize);
                    }

                    ImGui.TableSetColumnIndex(col++); RenderFlagCell("Mosaic", sprite.Mosaic);
                    ImGui.TableSetColumnIndex(col++); RenderFlagCell("8bpp", sprite.Color256);

                    ImGui.PopFont();
                    ImGui.EndTable();
                }

                if (ImGui.BeginTable("##sprprops", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Property");
                    ImGui.TableSetupColumn("Value");
                    ImGui.TableHeadersRow();

                    ImGui.PushFont(_monoFont);

                    numStr = StackString.Interpolate(numBuf, $"({sprite.X}, {sprite.Y})");
                    RenderPropertyRowText("Position", numStr.AsSpan());

                    numStr = StackString.Interpolate(numBuf, $"{sprite.Width}x{sprite.Height}");
                    RenderPropertyRowText("Size", numStr.AsSpan());

                    RenderPropertyRowText("Shape", ShapeNames[sprite.Shape]);

                    numStr = StackString.Interpolate(numBuf, $"0x{sprite.TileIndex:X3}");
                    RenderPropertyRowText("Tile", numStr.AsSpan());

                    RenderPropertyRow("Priority", numBuf, ref numStr, sprite.Priority);

                    RenderPropertyRowText("Graphics Mode", GfxModeNames[sprite.GfxMode]);

                    if (!sprite.Color256)
                        RenderPropertyRow("Palette", numBuf, ref numStr, sprite.Palette);

                    if (sprite.Affine)
                    {
                        RenderPropertyRow("Affine Group", numBuf, ref numStr, sprite.AffineIndex);

                        var (pa, pb, pc, pd) = snapshot.GetAffineParams(sprite.AffineIndex);
                        numStr = StackString.Interpolate(numBuf, $"{pa / 256f:F4}");
                        RenderPropertyRowText("PA", numStr.AsSpan());
                        numStr = StackString.Interpolate(numBuf, $"{pb / 256f:F4}");
                        RenderPropertyRowText("PB", numStr.AsSpan());
                        numStr = StackString.Interpolate(numBuf, $"{pc / 256f:F4}");
                        RenderPropertyRowText("PC", numStr.AsSpan());
                        numStr = StackString.Interpolate(numBuf, $"{pd / 256f:F4}");
                        RenderPropertyRowText("PD", numStr.AsSpan());
                    }

                    ImGui.PopFont();
                    ImGui.EndTable();
                }
            }

            if (isDisabled) ImGui.PopStyleColor();

            ImGui.PopID();
        }

        ImGui.End();
    }


    private void RenderSpritePreview(int spriteIndex, ref SpriteSnapshot.SpriteEntry sprite)
    {
        if (sprite.ObjMode == 2 || _visibleSlot >= SpritesPerPage) return;

        if (_renderSprite(spriteIndex, _pixelBuffer, out int w, out int h) && w > 0 && h > 0)
        {
            int slot = _visibleSlot++;
            EnsureTexture(slot, w, h);

            GL.BindTexture(TextureTarget.Texture2D, _spriteTextures[slot]);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, w, h,
                PixelFormat.Bgra, PixelType.UnsignedByte, _pixelBuffer);

            float scale = MathF.Max(1f, 64f / MathF.Max(w, h));
            ImGui.Image(_spriteTextures[slot], new Vector2(w * scale, h * scale));
        }
    }

    private void EnsureTexture(int slot, int width, int height)
    {
        if (_spriteTextures[slot] == 0)
        {
            _spriteTextures[slot] = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _spriteTextures[slot]);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }

        if (_texWidths[slot] != width || _texHeights[slot] != height)
        {
            GL.BindTexture(TextureTarget.Texture2D, _spriteTextures[slot]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, nint.Zero);

            _texWidths[slot]  = width;
            _texHeights[slot] = height;
        }
    }
}