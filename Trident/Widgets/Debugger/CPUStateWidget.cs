using ImGuiNET;
using System.Numerics;
using Trident.Utilities;
using Trident.Core.CPU.Registers;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Widgets.Debugger
{
    internal class CPUStateWidget : IWidget
    {
        private readonly Func<CPUSnapshot> _getSnapshot;
        private CPUSnapshot? _previousSnapshot;

        internal CPUStateWidget(Func<CPUSnapshot> getSnapshot)
        {
            _getSnapshot = getSnapshot;
        }

        public bool IsVisible { get; set; } = true;

        public string Name => "CPU State";
        public string Group => "CPU";

        public void Render()
        {
            if (!IsVisible) return;

            if (ImGui.Begin("CPU State"))
            {
                CPUSnapshot snapshot = _getSnapshot();

                ImGui.Columns(2, "reg_columns");
                for (int reg = 0; reg < 8; reg++)
                {
                    HighlightChange($"R{reg}", snapshot.Registers[reg], _previousSnapshot?.Registers[reg]);
                    ImGui.NextColumn();
                    HighlightChange($"R{reg + 8}", snapshot.Registers[reg + 8], _previousSnapshot?.Registers[reg + 8]);
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);

                ImGui.Separator();
                HighlightChange("CPSR", snapshot.CPSR, _previousSnapshot?.CPSR);

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

                _previousSnapshot = snapshot;
            }

            ImGui.End();
        }

        private void HighlightChange(string label, uint current, uint? previous)
        {
            if (previous.HasValue && current != previous.Value)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.85f, 0.80f, 0.95f, 1f));
                ImGui.Text($"{label}: 0x{current:X8}");
                ImGui.PopStyleColor();
            }
            else
                ImGui.Text($"{label}: 0x{current:X8}");
        }
    }
}