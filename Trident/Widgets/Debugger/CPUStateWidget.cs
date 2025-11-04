using ImGuiNET;
using System.Numerics;
using Trident.Core.CPU;
using Trident.Utilities;
using Trident.Core.CPU.Registers;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Widgets.Debugger
{
    internal class CPUStateWidget : IWidget
    {
        private readonly Func<CPUSnapshot> _getSnapshot;
        private CPUSnapshot? _previousSnapshot;
        private uint _previousSPSR;

        private readonly ImFontPtr _monoFont;

        private readonly Vector4 _lavender = new(0.87f, 0.82f, 0.97f, 1f);
        private readonly uint _tableHighlight = ImGui.ColorConvertFloat4ToU32(new(0.20f, 0.18f, 0.28f, 0.90f));

        private readonly (string Label, int Bit)[] _flags =
        [
            ("N", 31), ("Z", 30), ("C", 29), ("V", 28),
            ("I", 7), ("F", 6), ("T", 5)
        ];

        internal CPUStateWidget(ImFontPtr monoFont, Func<CPUSnapshot> getSnapshot)
        {
            _getSnapshot = getSnapshot;
            _monoFont = monoFont;
        }

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

                        HighlightChange(
                            $"R{regIndex}",
                            snapshot.Registers[regIndex],
                            _previousSnapshot?.Registers[regIndex],
                            4
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
                HighlightChange("CPSR", snapshot.CPSR, _previousSnapshot?.CPSR, 5);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextDisabled("SPSR");
                ImGui.SameLine();

                if (!RegisterSet.IsUserOrSystem(snapshot.Mode))
                {
                    ImGui.Text($"0x{snapshot.SPSR:X8}");
                    _previousSPSR = snapshot.SPSR;
                }
                else
                    ImGui.TextDisabled($"0x{_previousSPSR:X8}");

                ImGui.EndTable();
            }


            if (ImGui.BeginTable("FlagsTable", _flags.Length, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableNextRow();

                for (int col = 0; col < _flags.Length; col++)
                {
                    ImGui.TableSetColumnIndex(col);

                    if (snapshot.CPSR.IsBitSet(_flags[col].Bit))
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, _tableHighlight);

                    ImGui.Text(_flags[col].Label);
                }

                ImGui.EndTable();
            }

            ImGui.Text($"Mode: {snapshot.Mode}");

            _previousSnapshot = snapshot;

            ImGui.PopFont();
            ImGui.End();
        }


        private void HighlightChange(string label, uint current, uint? previous, int totalLabelWidth = 0)
        {
            string paddedLabel = label.PadRight(totalLabelWidth);
            ImGui.TextDisabled(paddedLabel);
            ImGui.SameLine();

            if (previous.HasValue && current != previous.Value)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, _lavender);
                ImGui.Text($"0x{current:X8}");
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.Text($"0x{current:X8}");
            }
        }

        private bool IsBanked(int reg, ProcessorMode mode) => mode switch
        {
            ProcessorMode.FIQ => reg >= 8 && reg <= 14,

            ProcessorMode.IRQ or
            ProcessorMode.SVC or
            ProcessorMode.ABT or
            ProcessorMode.UND =>
                reg == 13 || reg == 14,

            _ => false
        };
    }
}