using ImGuiNET;
using Trident.Utilities;
using Trident.Core.Debugging.Snapshots;

using static Trident.Widgets.WidgetHelpers;

namespace Trident.Widgets.Debugger;

internal class EffectsWidget(ImFontPtr monoFont, Func<EffectsSnapshot> getSnapshot) : IWidget
{
    private readonly Func<EffectsSnapshot> _getSnapshot = getSnapshot;
    private readonly ImFontPtr _monoFont                = monoFont;

    private static readonly string[] LayerNames = ["BG0", "BG1", "BG2", "BG3", "OBJ", "BD"];
    private static readonly string[] BlendNames = ["Off", "Alpha", "Brighten", "Darken"];

    private const ImGuiTableFlags TableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;


    public bool IsVisible { get; set; } = true;

    public string Name  => "Window/Mosaic/Blend";
    public string Group => "PPU";

    public void Render()
    {
        if (!IsVisible) return;

        if (!ImGui.Begin("Window/Mosaic/Blend"))
        {
            ImGui.End();
            return;
        }

        EffectsSnapshot snapshot = _getSnapshot();
        Span<char> buf           = stackalloc char[48];

        if (ImGui.BeginTabBar("##fxtabs", ImGuiTabBarFlags.FittingPolicyScroll))
        {
            if (ImGui.BeginTabItem("Windows"))
            {
                RenderWindowsTab(snapshot, buf);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Mosaic"))
            {
                RenderMosaicTab(snapshot.Mosaic, buf);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Blend"))
            {
                RenderBlendTab(snapshot.Blend, buf);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
    }


    private void RenderWindowsTab(in EffectsSnapshot snapshot, Span<char> buf)
    {
        RenderWindowSection(0, "WIN0", snapshot.Window0Enabled, snapshot.Window0, buf);
        RenderWindowSection(1, "WIN1", snapshot.Window1Enabled, snapshot.Window1, buf);

        RenderRegionSection(2, "Outside", true, snapshot.Outside);
        RenderRegionSection(3, "OBJ Window", snapshot.ObjWindowEnabled, snapshot.ObjWindow);
    }

    private void RenderWindowSection(int id, ReadOnlySpan<char> name, bool enabled, in EffectsSnapshot.WindowSnapshot window, Span<char> buf)
    {
        ImGui.TextDisabled(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(StackString.Interpolate(buf, $"X: [{window.Left}, {window.Right}) Y: [{window.Top}, {window.Bottom})").AsSpan());
        RenderRegionFlags(id, enabled, window.Region);
    }

    private void RenderRegionSection(int id, ReadOnlySpan<char> name, bool enabled, in EffectsSnapshot.RegionSnapshot region)
    {
        ImGui.TextDisabled(name);
        RenderRegionFlags(id, enabled, region);
    }

    private void RenderRegionFlags(int id, bool enabled, in EffectsSnapshot.RegionSnapshot region)
    {
        ImGui.PushID(id);

        if (ImGui.BeginTable("##regionflags", 7, TableFlags))
        {
            ImGui.TableNextRow();
            ImGui.PushFont(_monoFont);

            ImGui.TableSetColumnIndex(0); RenderFlagCell("On", enabled);

            for (int i = 0; i < 4; i++)
            {
                ImGui.TableSetColumnIndex(1 + i);
                RenderFlagCell(LayerNames[i], (region.Layers & (1 << i)) != 0);
            }

            ImGui.TableSetColumnIndex(5); RenderFlagCell("OBJ", region.ObjEnabled);
            ImGui.TableSetColumnIndex(6); RenderFlagCell("FX",  region.EffectsEnabled);

            ImGui.PopFont();
            ImGui.EndTable();
        }

        ImGui.PopID();
    }


    private void RenderMosaicTab(in EffectsSnapshot.MosaicSnapshot mosaic, Span<char> buf)
    {
        if (!ImGui.BeginTable("##mosaic", 2, TableFlags))
            return;

        ImGui.TableSetupColumn("Layer");
        ImGui.TableSetupColumn("Size (H x V)");
        ImGui.TableHeadersRow();

        ImGui.PushFont(_monoFont);

        RenderPropertyRowText("BG",  StackString.Interpolate(buf, $"{mosaic.BGHSize + 1} x {mosaic.BGVSize + 1}").AsSpan());
        RenderPropertyRowText("OBJ", StackString.Interpolate(buf, $"{mosaic.OBJHSize + 1} x {mosaic.OBJVSize + 1}").AsSpan());

        ImGui.PopFont();
        ImGui.EndTable();
    }


    private void RenderBlendTab(in EffectsSnapshot.BlendSnapshot blend, Span<char> buf)
    {
        RenderTargetFlags("##blend1st", "1st", blend.FirstTargets);
        RenderTargetFlags("##blend2nd", "2nd", blend.SecondTargets);

        if (!ImGui.BeginTable("##blendprops", 2, TableFlags))
            return;

        ImGui.TableSetupColumn("Property");
        ImGui.TableSetupColumn("Value");
        ImGui.TableHeadersRow();

        ImGui.PushFont(_monoFont);

        RenderPropertyRowText("Mode", BlendNames[(int)blend.Mode]);
        RenderPropertyRowText("EVA",  StackString.Interpolate(buf, $"{blend.EVA}/16").AsSpan());
        RenderPropertyRowText("EVB",  StackString.Interpolate(buf, $"{blend.EVB}/16").AsSpan());
        RenderPropertyRowText("EVY",  StackString.Interpolate(buf, $"{blend.EVY}/16").AsSpan());

        ImGui.PopFont();
        ImGui.EndTable();
    }

    private void RenderTargetFlags(string id, ReadOnlySpan<char> title, byte mask)
    {
        ImGui.TextDisabled(title);

        if (!ImGui.BeginTable(id, LayerNames.Length, TableFlags))
            return;

        ImGui.TableNextRow();
        ImGui.PushFont(_monoFont);

        for (int i = 0; i < LayerNames.Length; i++)
        {
            ImGui.TableSetColumnIndex(i);
            RenderFlagCell(LayerNames[i], (mask & (1 << i)) != 0);
        }

        ImGui.PopFont();
        ImGui.EndTable();
    }
}