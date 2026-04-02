using ImGuiNET;
using Trident.Utilities;
using Trident.Core.Debugging.Snapshots;

using static Trident.Widgets.WidgetHelpers;

namespace Trident.Widgets.Debugger;

internal class TimerWidget(ImFontPtr monoFont, Func<TimerSnapshot> getSnapshot) : IWidget
{
    private readonly Func<TimerSnapshot> _getSnapshot = getSnapshot;
    private readonly ImFontPtr _monoFont              = monoFont;

    private readonly string[] _prescalerLabels = ["F/1", "F/64", "F/256", "F/1024"];
    private readonly string[] _headers         = ["Timer 0", "Timer 1", "Timer 2", "Timer 3"];


    public bool IsVisible { get; set; } = true;

    public string Name  => "Timers";
    public string Group => "System";

    public void Render()
    {
        if (!IsVisible) return;

        if (!ImGui.Begin("Timers"))
        {
            ImGui.End();
            return;
        }

        TimerSnapshot snapshot = _getSnapshot();

        Span<TimerSnapshot.ChannelSnapshot> channels =
        [
            snapshot.Channel0,
            snapshot.Channel1,
            snapshot.Channel2,
            snapshot.Channel3
        ];

        Span<char> numBuf = stackalloc char[12];
        StackString numStr = new(numBuf);

        for (int i = 0; i < channels.Length; i++)
        {
            var ch = channels[i];

            if (ImGui.CollapsingHeader(_headers[i]))
            {
                if (ImGui.BeginTable($"##tmrvals", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("State");
                    ImGui.TableSetupColumn("Control");
                    ImGui.TableHeadersRow();

                    ImGui.TableNextRow();

                    ImGui.PushFont(_monoFont);
                    ImGui.TableSetColumnIndex(0);

                    ImGui.TextDisabled("CNT "); ImGui.SameLine();
                    numStr = StackString.Interpolate(numBuf, $"0x{ch.Counter:X4}");
                    ImGui.TextUnformatted(numStr.AsSpan());

                    ImGui.TextDisabled("RLD "); ImGui.SameLine();
                    numStr = StackString.Interpolate(numBuf, $"0x{ch.Reload:X4}");
                    ImGui.TextUnformatted(numStr.AsSpan());

                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextDisabled("PRE "); ImGui.SameLine();
                    ImGui.TextUnformatted(_prescalerLabels[ch.Prescaler]);

                    if (i > 0)
                    {
                        ImGui.TextDisabled("CSC "); ImGui.SameLine();
                        ImGui.TextUnformatted(ch.Cascade ? "Count-up" : "Normal");
                    }

                    ImGui.PopFont();

                    ImGui.EndTable();
                }

                int flagCount = (i > 0) ? 4 : 3;
                if (ImGui.BeginTable($"##tmrflags", flagCount, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableNextRow();

                    ImGui.PushFont(_monoFont);
                    ImGui.TableSetColumnIndex(0); RenderFlagCell("Enable", ch.Enabled);
                    ImGui.TableSetColumnIndex(1); RenderFlagCell("Running", ch.Running);
                    ImGui.TableSetColumnIndex(2); RenderFlagCell("IRQ", ch.IRQEnabled);
                    if (i > 0) { ImGui.TableSetColumnIndex(3); RenderFlagCell("Cascade", ch.Cascade); }
                    ImGui.PopFont();

                    ImGui.EndTable();
                }
            }
        }

        ImGui.End();
    }
}