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

        private readonly ImFontPtr _monoFont;

        private readonly Vector4 _lavender = new(0.87f, 0.82f, 0.97f, 1f);
        private readonly uint _tableHighlight = ImGui.ColorConvertFloat4ToU32(new(0.20f, 0.18f, 0.28f, 0.90f));

        internal CPUStateWidget(Func<CPUSnapshot> getSnapshot, ImFontPtr monoFont)
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
                return;


            CPUSnapshot snapshot = _getSnapshot();

            ImGui.PushFont(_monoFont);

            if (ImGui.BeginTable("RegisterTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                for (int row = 0; row < 8; row++)
                {
                    ImGui.TableNextRow();

                    int regLow = row;
                    ImGui.TableSetColumnIndex(0);
                    HighlightChange($"R{regLow}:", snapshot.Registers[regLow], _previousSnapshot?.Registers[regLow], 4);


                    int regHigh = row + 8;
                    ImGui.TableSetColumnIndex(1);
                    if (IsBanked(regHigh, snapshot.Mode))
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, _tableHighlight);

                    HighlightChange($"R{regHigh}:", snapshot.Registers[regHigh], _previousSnapshot?.Registers[regHigh], 5);
                }
                ImGui.EndTable();
            }

            ImGui.Separator();
            HighlightChange("CPSR:", snapshot.CPSR, _previousSnapshot?.CPSR, 6);

            bool n = snapshot.CPSR.IsBitSet(31);
            bool z = snapshot.CPSR.IsBitSet(30);
            bool c = snapshot.CPSR.IsBitSet(29);
            bool v = snapshot.CPSR.IsBitSet(28);
            bool i = snapshot.CPSR.IsBitSet(7);
            bool f = snapshot.CPSR.IsBitSet(6);
            bool t = snapshot.CPSR.IsBitSet(5);

            ImGui.BeginDisabled();
            ImGui.Checkbox("N", ref n);
            ImGui.SameLine(); ImGui.Checkbox("Z", ref z);
            ImGui.SameLine(); ImGui.Checkbox("C", ref c);
            ImGui.SameLine(); ImGui.Checkbox("V", ref v);
            ImGui.Checkbox("I", ref i);
            ImGui.SameLine(); ImGui.Checkbox("F", ref f);
            ImGui.SameLine(); ImGui.Checkbox("T", ref t);
            ImGui.EndDisabled();

            ImGui.Text($"Mode: {snapshot.Mode}");

            if (!RegisterSet.IsUserOrSystem(snapshot.Mode))
            {
                ImGui.Separator();
                ImGui.Text($"SPSR: 0x{snapshot.SPSR:X8}");
            }
            ImGui.PopFont();

            _previousSnapshot = snapshot;

            ImGui.End();
        }


        private void HighlightChange(string label, uint current, uint? previous, int totalLabelWidth = 0)
        {
            string paddedLabel = label.PadRight(totalLabelWidth);

            if (previous.HasValue && current != previous.Value)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, _lavender);
                ImGui.Text($"{paddedLabel}0x{current:X8}");
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.Text($"{paddedLabel}0x{current:X8}");
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