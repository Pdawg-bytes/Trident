using ImGuiNET;
using System.Numerics;
using Trident.Utilities;

namespace Trident.Widgets;

internal static class WidgetHelpers
{
    internal static void RenderFlagCell(ReadOnlySpan<char> label, bool state)
    {
        if (state)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, Color.HiglightBackground);

        Vector2 cellSize = ImGui.GetContentRegionAvail();
        Vector2 textSize = ImGui.CalcTextSize(label);
        float xOffset    = (cellSize.X - textSize.X) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + xOffset);

        ImGui.TextUnformatted(label);
    }


    internal static void RenderPropertyRow<T>(ReadOnlySpan<char> label, Span<char> buf, ref StackString str, T value) where T : ISpanFormattable
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(1);
        str.Reset();
        str.AppendFormatted(value);
        ImGui.TextUnformatted(str.AsSpan());
    }

    internal static void RenderPropertyRowText(ReadOnlySpan<char> label, ReadOnlySpan<char> value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(value);
    }


    internal static void RenderCenteredCell(ReadOnlySpan<char> text)
    {
        Vector2 cellSize = ImGui.GetContentRegionAvail();
        Vector2 textSize = ImGui.CalcTextSize(text);
        float xOffset    = (cellSize.X - textSize.X) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + xOffset);

        ImGui.TextUnformatted(text);
    }
}