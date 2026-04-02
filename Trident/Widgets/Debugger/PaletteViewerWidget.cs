using ImGuiNET;
using System.Numerics;
using Trident.Utilities;
using Trident.Core.Debugging.Snapshots;

using static Trident.Widgets.WidgetHelpers;

namespace Trident.Widgets.Debugger;

internal class PaletteViewerWidget(ImFontPtr monoFont, Func<PaletteSnapshot> getSnapshot) : IWidget
{
    private readonly Func<PaletteSnapshot> _getSnapshot = getSnapshot;
    private readonly ImFontPtr _monoFont                = monoFont;

    private const float SwatchSize    = 14f;
    private const float SwatchSpacing = 1f;
    private const int ColorsPerRow    = 16;
    private const int PalettesPerBank = 16;

    private int _selectedIndex = -1;
    private int _activePage    = 0;

    private readonly string[] PageNames = ["Background", "Sprite"];


    public bool IsVisible { get; set; } = true;

    public string Name  => "Palette Viewer";
    public string Group => "PPU";

    public void Render()
    {
        if (!IsVisible) return;

        if (!ImGui.Begin("Palette Viewer"))
        {
            ImGui.End();
            return;
        }

        PaletteSnapshot snapshot = _getSnapshot();

        if (ImGui.BeginTabBar("##paltabs"))
        {
            for (int page = 0; page < 2; page++)
            {
                if (ImGui.BeginTabItem(PageNames[page]))
                {
                    _activePage    = page;
                    int baseOffset = page * 256;

                    float gridWidth = ColorsPerRow * (SwatchSize + SwatchSpacing);

                    if (ImGui.BeginTable("##pallayout", 2, ImGuiTableFlags.None))
                    {
                        ImGui.TableSetupColumn("Grid", ImGuiTableColumnFlags.WidthFixed, gridWidth + 8f);
                        ImGui.TableSetupColumn("Detail", ImGuiTableColumnFlags.WidthStretch);

                        ImGui.TableNextRow();

                        ImGui.TableSetColumnIndex(0);
                        RenderPaletteGrid(snapshot, baseOffset);

                        ImGui.TableSetColumnIndex(1);
                        if (_selectedIndex >= 0)
                            RenderColorDetail(snapshot);

                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }

        ImGui.End();
    }


    private void RenderPaletteGrid(PaletteSnapshot snapshot, int baseOffset)
    {
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        Vector2 origin         = ImGui.GetCursorScreenPos();

        float totalStep = SwatchSize + SwatchSpacing;

        for (int palette = 0; palette < PalettesPerBank; palette++)
        {
            for (int color = 0; color < ColorsPerRow; color++)
            {
                int index = baseOffset + palette * ColorsPerRow + color;

                float x = origin.X + color * totalStep;
                float y = origin.Y + palette * totalStep;

                Vector2 min = new(x, y);
                Vector2 max = new(x + SwatchSize, y + SwatchSize);

                ushort bgr555 = snapshot.GetColor(index);
                uint rgba     = Bgr555ToRgba(bgr555);

                drawList.AddRectFilled(min, max, rgba);

                if (index == _selectedIndex)
                    drawList.AddRect(min, max, 0xFFFFFFFF, 0, ImDrawFlags.None, 2f);
            }
        }

        Vector2 mousePos = ImGui.GetMousePos();
        float gridWidth  = ColorsPerRow    * totalStep;
        float gridHeight = PalettesPerBank * totalStep;

        ImGui.Dummy(new Vector2(gridWidth, gridHeight));

        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            float relX = mousePos.X - origin.X;
            float relY = mousePos.Y - origin.Y;

            int col = (int)(relX / totalStep);
            int row = (int)(relY / totalStep);

            if (col >= 0 && col < ColorsPerRow && row >= 0 && row < PalettesPerBank)
                _selectedIndex = _activePage * 256 + row * ColorsPerRow + col;
        }
    }

    private void RenderColorDetail(PaletteSnapshot snapshot)
    {
        ushort bgr555 = snapshot.GetColor(_selectedIndex);

        int r5 = bgr555 & 0x1F;
        int g5 = (bgr555 >> 5) & 0x1F;
        int b5 = (bgr555 >> 10) & 0x1F;

        int r8 = (r5 << 3) | (r5 >> 2);
        int g8 = (g5 << 3) | (g5 >> 2);
        int b8 = (b5 << 3) | (b5 >> 2);

        Vector4 colorVec = new(r8 / 255f, g8 / 255f, b8 / 255f, 1f);
        ImGui.ColorButton("##preview", colorVec, ImGuiColorEditFlags.NoTooltip, new Vector2(44, 44));

        Span<char> buf  = stackalloc char[48];
        StackString str = new(buf);

        int localIndex = _selectedIndex & 0xFF;
        int bank       = _selectedIndex >= 256 ? 1 : 0;
        int paletteNum = localIndex >> 4;
        int colorNum   = localIndex &  0x0F;

        if (ImGui.BeginTable("##coldetail", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Property");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            ImGui.PushFont(_monoFont);

            RenderPropertyRowText("Bank", bank == 0 ? "BG" : "OBJ");

            str.Reset();
            str.AppendFormatted(paletteNum);
            RenderPropertyRowText("Palette", str.AsSpan());

            str.Reset();
            str.AppendFormatted(colorNum);
            RenderPropertyRowText("Color", str.AsSpan());

            str = StackString.Interpolate(buf, $"0x{_selectedIndex:X3}");
            RenderPropertyRowText("Index", str.AsSpan());

            str = StackString.Interpolate(buf, $"0x{bgr555:X4}");
            RenderPropertyRowText("Raw", str.AsSpan());

            str = StackString.Interpolate(buf, $"{r5}, {g5}, {b5}");
            RenderPropertyRowText("RGB (5)", str.AsSpan());

            str = StackString.Interpolate(buf, $"{r8}, {g8}, {b8}");
            RenderPropertyRowText("RGB (8)", str.AsSpan());

            ImGui.PopFont();
            ImGui.EndTable();
        }
    }


    private static uint Bgr555ToRgba(ushort bgr555)
    {
        int r = bgr555 & 0x1F;
        int g = (bgr555 >> 5) & 0x1F;
        int b = (bgr555 >> 10) & 0x1F;

        uint r8 = (uint)((r << 3) | (r >> 2));
        uint g8 = (uint)((g << 3) | (g >> 2));
        uint b8 = (uint)((b << 3) | (b >> 2));

        return 0xFF000000 | (b8 << 16) | (g8 << 8) | r8;
    }
}