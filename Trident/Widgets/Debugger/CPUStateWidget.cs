using ImGuiNET;
using System.Numerics;
using Trident.Core.CPU;
using Trident.Utilities;
using Trident.Core.CPU.Registers;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Widgets.Debugger;

internal class CPUStateWidget(ImFontPtr monoFont, Func<CPUSnapshot> getSnapshot) : IWidget
{
    private readonly Func<CPUSnapshot> _getSnapshot = getSnapshot;
    private CPUSnapshot? _previousSnapshot;
    private uint _previousSPSR;

    private readonly ImFontPtr _monoFont = monoFont;

    private readonly Vector4 _updateHighlight = Color.MakeHighlight(ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark]);
    private readonly uint _tableHighlight = ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark]) & 0x3FFFFFFF;

    private readonly (char Label, int Bit)[] _flags =
    [
        ('N', 31), ('Z', 30), ('C', 29), ('V', 28),
        ('I', 7), ('F', 6), ('T', 5)
    ];


    public bool IsVisible { get; set; } = true;

    public string Name => "CPU State";
    public string Group => "CPU";

    public void Render()
    {
        if (!IsVisible) return;

        if (!ImGui.Begin("CPU State"))
        {
            ImGui.End(); 
            return;
        }


        CPUSnapshot snapshot = _getSnapshot();

        ImGui.PushFont(_monoFont);

        const int cols = 3;
        if (ImGui.BeginTable("RegisterTable", cols, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            const int totalRegs = 16;
            int rows = (int)Math.Ceiling(totalRegs / (float)cols);

            Span<char> regBuf = stackalloc char[6];
            for (int row = 0; row < rows; row++)
            {
                ImGui.TableNextRow();

                for (int col = 0; col < cols; col++)
                {
                    int regIndex = row * cols + col;
                    if (regIndex >= totalRegs)
                        break;

                    ImGui.TableSetColumnIndex(col);

                    if (IsBanked(regIndex, snapshot.Mode))
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, _tableHighlight);

                    var regStr = StackString.Interpolate(regBuf, $"R{regIndex}");

                    HighlightChange
                    (
                        regStr,
                        snapshot.Registers[regIndex],
                        _previousSnapshot?.Registers[regIndex],
                        regBuf.Length
                    );
                }
            }

            ImGui.EndTable();
        }

        ImGui.Separator();

        if (ImGui.BeginTable("StatusRegs", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            Span<char> buf = stackalloc char[6];
            var s = StackString.From("CPSR", buf);
            HighlightChange(s, snapshot.CPSR, _previousSnapshot?.CPSR, buf.Length);

            ImGui.TableSetColumnIndex(1);

            ImGui.TextDisabled("SPSR");
            ImGui.SameLine();

            Span<char> spsrBuf = stackalloc char[20];
            var spsrStr = new StackString(spsrBuf);
            if (!RegisterSet.IsUserOrSystem(snapshot.Mode))
            {
                spsrStr = StackString.Interpolate(spsrBuf, $"0x{snapshot.SPSR:X8}");
                ImGui.TextUnformatted(spsrStr.AsSpan());

                _previousSPSR = snapshot.SPSR;
            }
            else
            {
                spsrStr = StackString.Interpolate(spsrBuf, $"0x{_previousSPSR:X8}");
                ImGui.TextDisabled(spsrStr.AsSpan());
            }

            ImGui.EndTable();
        }

        if (ImGui.BeginTable("FlagsTable", _flags.Length, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableNextRow();

            Span<char> flagBuf = stackalloc char[1];
            for (int col = 0; col < _flags.Length; col++)
            {
                ImGui.TableSetColumnIndex(col);

                if (snapshot.CPSR.IsBitSet(_flags[col].Bit))
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, _tableHighlight);

                flagBuf[0] = _flags[col].Label;
                Vector2 cellSize = ImGui.GetContentRegionAvail();
                Vector2 textSize = ImGui.CalcTextSize(flagBuf);
                float xOffset = (cellSize.X - textSize.X) * 0.5f;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + xOffset);
                ImGui.TextUnformatted(flagBuf);
            }

            ImGui.EndTable();
        }

        Span<char> modeBuf = stackalloc char[9];
        var modeStr = new StackString(modeBuf);
        modeStr.Append("Mode: ");
        modeStr.Append(ModeToString(snapshot.Mode));
        ImGui.TextUnformatted(modeBuf);

        _previousSnapshot = snapshot;

        ImGui.PopFont();
        ImGui.End();
    }


    private void HighlightChange(StackString label, uint current, uint? previous, int totalLabelWidth = 0)
    {
        label.PadRight(totalLabelWidth);

        ImGui.TextDisabled(label.AsSpan());
        ImGui.SameLine();

        Span<char> buffer = stackalloc char[20];
        var valueStr = StackString.Interpolate(buffer, $"0x{current:X8}");

        if (previous.HasValue && current != previous.Value)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, _updateHighlight);
            ImGui.TextUnformatted(valueStr.AsSpan());
            ImGui.PopStyleColor();
        }
        else
            ImGui.TextUnformatted(valueStr.AsSpan());
    }


    private static bool IsBanked(int reg, ProcessorMode mode) => mode switch
    {
        ProcessorMode.FIQ => reg >= 8 && reg <= 14,

        ProcessorMode.IRQ or
        ProcessorMode.SVC or
        ProcessorMode.ABT or
        ProcessorMode.UND =>
            reg == 13 || reg == 14,

        _ => false
    };

    private static ReadOnlySpan<char> ModeToString(ProcessorMode mode) => mode switch
    {
        ProcessorMode.USR => "USR",
        ProcessorMode.SYS => "SYS",
        ProcessorMode.SVC => "SVC",

        ProcessorMode.FIQ => "FIQ",
        ProcessorMode.IRQ => "IRQ",

        ProcessorMode.ABT => "ABT",
        ProcessorMode.UND => "UND",

        _ => "???"
    };
}