using ImGuiNET;

namespace Trident.Interaction;

internal static class Tooltips
{
    internal static void HelpTooltip(string tooltip, float offset = 0)
    {
        ImGui.SameLine(ImGui.GetContentRegionAvail().X + offset);
        ImGui.TextDisabled("(?)");
        if (ImGui.BeginItemTooltip())
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(tooltip);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }
}