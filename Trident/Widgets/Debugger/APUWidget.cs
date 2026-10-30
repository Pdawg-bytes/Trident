using ImGuiNET;
using System.Numerics;
using Trident.Utilities;
using Trident.Core.Machine;
using Trident.Core.Debugging.Snapshots;

using static Trident.Widgets.WidgetHelpers;

namespace Trident.Widgets.Debugger;

internal delegate void AudioScopeReader(int source, Span<short> destination);

internal class APUWidget(ImFontPtr monoFont, Func<APUSnapshot> getSnapshot, AudioScopeReader readScope) : IWidget
{
    private const int ScopeSources = 8;
    private const int FIFOA = 0, FIFOB = 1, PSGBase = 2, MasterLeft = 6, MasterRight = 7;

    private readonly Func<APUSnapshot> _getSnapshot = getSnapshot;
    private readonly AudioScopeReader _readScope    = readScope;
    private readonly ImFontPtr _monoFont            = monoFont;

    private readonly float[][] _scopes     = [.. Enumerable.Range(0, ScopeSources).Select(_ => new float[GBA.AudioScopeLength])];
    private readonly short[] _scopeScratch = new short[GBA.AudioScopeLength];

    private readonly string[] _psgHeaders  = [ "Square 1", "Square 2", "Wave", "Noise" ];
    private readonly string[] _fifoHeaders = [ "Direct Sound A", "Direct Sound B" ];
    private readonly string[] _psgVolumes  = [ "25%", "50%", "100%", "Prohibited" ];


    public bool IsVisible { get; set; } = true;

    public string Name  => "APU";
    public string Group => "System";

    public void Render()
    {
        if (!IsVisible) return;

        if (!ImGui.Begin("APU"))
        {
            ImGui.End();
            return;
        }

        APUSnapshot snapshot = _getSnapshot();

        for (int i = 0; i < ScopeSources; i++)
        {
            _readScope(i, _scopeScratch);

            float[] scope = _scopes[i];
            for (int s = 0; s < scope.Length; s++)
                scope[s] = _scopeScratch[s];
        }

        Span<char> numBuf  = stackalloc char[12];
        StackString numStr = new(numBuf);

        if (ImGui.CollapsingHeader("Master", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.BeginTable("##apumaster", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Output");
                ImGui.TableSetupColumn("Mixing");
                ImGui.TableHeadersRow();

                ImGui.TableNextRow();

                ImGui.PushFont(_monoFont);
                ImGui.TableSetColumnIndex(0);

                ImGui.TextDisabled("RATE "); ImGui.SameLine();
                numStr = StackString.Interpolate(numBuf, $"{snapshot.SampleRate} Hz");
                ImGui.TextUnformatted(numStr.AsSpan());

                ImGui.TextDisabled("BIAS "); ImGui.SameLine();
                numStr = StackString.Interpolate(numBuf, $"0x{snapshot.BiasLevel:X3}");
                ImGui.TextUnformatted(numStr.AsSpan());

                ImGui.TableSetColumnIndex(1);
                ImGui.TextDisabled("PSG "); ImGui.SameLine();
                ImGui.TextUnformatted(_psgVolumes[snapshot.PSGVolume]);

                ImGui.TextDisabled("VOL "); ImGui.SameLine();
                numStr = StackString.Interpolate(numBuf, $"L{snapshot.VolumeLeft} R{snapshot.VolumeRight}");
                ImGui.TextUnformatted(numStr.AsSpan());
                ImGui.PopFont();

                ImGui.EndTable();
            }

            ImGui.TextDisabled("Left");
            RenderScope("##masterl", MasterLeft, -512f, 512f);
            ImGui.TextDisabled("Right");
            RenderScope("##masterr", MasterRight, -512f, 512f);

            if (ImGui.BeginTable("##apuflags", 1, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableNextRow();

                ImGui.PushFont(_monoFont);
                ImGui.TableSetColumnIndex(0); RenderFlagCell("Master Enable", snapshot.MasterEnable);
                ImGui.PopFont();

                ImGui.EndTable();
            }
        }

        RenderFIFO(0, FIFOA, snapshot.FIFOA, numBuf);
        RenderFIFO(1, FIFOB, snapshot.FIFOB, numBuf);

        RenderPSG(0, snapshot.Square1, numBuf);
        RenderPSG(1, snapshot.Square2, numBuf);
        RenderPSG(2, snapshot.Wave, numBuf);
        RenderPSG(3, snapshot.Noise, numBuf);

        ImGui.End();
    }


    private void RenderScope(string id, int source, float min, float max)
    {
        float[] scope = _scopes[source];
        ImGui.PlotLines(id, ref scope[0], scope.Length, 0, null, min, max,
            new Vector2(ImGui.GetContentRegionAvail().X, 50));
    }

    private void RenderFIFO(int index, int source, in APUSnapshot.FIFOSnapshot fifo, Span<char> numBuf)
    {
        if (!ImGui.CollapsingHeader(_fifoHeaders[index]))
            return;

        StackString numStr = new(numBuf);

        RenderScope($"##fifoscope{index}", source, -128f, 128f);

        if (ImGui.BeginTable($"##fifo{index}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("State");
            ImGui.TableSetupColumn("Control");
            ImGui.TableHeadersRow();

            ImGui.TableNextRow();

            ImGui.PushFont(_monoFont);
            ImGui.TableSetColumnIndex(0);

            ImGui.TextDisabled("LEN "); ImGui.SameLine();
            numStr = StackString.Interpolate(numBuf, $"{fifo.Count}/32");
            ImGui.TextUnformatted(numStr.AsSpan());

            ImGui.TableSetColumnIndex(1);
            ImGui.TextDisabled("TMR "); ImGui.SameLine();
            numStr = StackString.Interpolate(numBuf, $"Timer {fifo.Timer}");
            ImGui.TextUnformatted(numStr.AsSpan());

            ImGui.TextDisabled("VOL "); ImGui.SameLine();
            ImGui.TextUnformatted(fifo.FullVolume ? "100%" : "50%");
            ImGui.PopFont();

            ImGui.EndTable();
        }

        if (ImGui.BeginTable($"##fifoflags{index}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableNextRow();

            ImGui.PushFont(_monoFont);
            ImGui.TableSetColumnIndex(0); RenderFlagCell("Left", fifo.EnableLeft);
            ImGui.TableSetColumnIndex(1); RenderFlagCell("Right", fifo.EnableRight);
            ImGui.PopFont();

            ImGui.EndTable();
        }
    }

    private void RenderPSG(int index, in APUSnapshot.PSGSnapshot psg, Span<char> numBuf)
    {
        if (!ImGui.CollapsingHeader(_psgHeaders[index]))
            return;

        RenderScope($"##psgscope{index}", PSGBase + index, 0f, 15f);

        if (ImGui.BeginTable($"##psgflags{index}", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableNextRow();

            ImGui.PushFont(_monoFont);
            ImGui.TableSetColumnIndex(0); RenderFlagCell("Active", psg.Active);
            ImGui.TableSetColumnIndex(1); RenderFlagCell("Length", psg.LengthEnabled);
            ImGui.TableSetColumnIndex(2); RenderFlagCell("Left", psg.EnableLeft);
            ImGui.TableSetColumnIndex(3); RenderFlagCell("Right", psg.EnableRight);
            ImGui.PopFont();

            ImGui.EndTable();
        }
    }
}