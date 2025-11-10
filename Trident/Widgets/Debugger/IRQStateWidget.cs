using ImGuiNET;
using System.Numerics;
using Trident.Utilities;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Widgets.Debugger
{
    internal class IRQStateWidget : IWidget
    {
        private readonly Func<IRQSnapshot> _getSnapshot;

        private readonly ImFontPtr _monoFont;
        private readonly Vector4 _lavender = new(0.87f, 0.82f, 0.97f, 1f);

        internal IRQStateWidget(ImFontPtr monoFont, Func<IRQSnapshot> getSnapshot)
        {
            _getSnapshot = getSnapshot;
            _monoFont = monoFont;
        }

        public bool IsVisible { get; set; } = true;

        public string Name => "Interrupt Controller";
        public string Group => "System";

        public void Render()
        {
            if (!IsVisible) return;

            if (!ImGui.Begin("Interrupts"))
            {
                ImGui.End();
                return; 
            }


            IRQSnapshot snapshot = _getSnapshot();

            ImGui.PushFont(_monoFont);

            Span<char> interruptBuf = stackalloc char[64];
            var interruptStr = new StackString(interruptBuf);
            interruptStr += "IME: ";
            interruptStr += snapshot.GlobalInterruptEnable ? "Enabled" : "Disabled";

            interruptStr += "\nIE:  0b";
            interruptStr.AppendBinary(snapshot.InterruptEnable, 16);

            interruptStr += "\nIF:  0b";
            interruptStr.AppendBinary(snapshot.InterruptFlag, 16);

            ImGui.TextUnformatted(interruptStr.AsSpan());
            ImGui.PopFont();

            if (ImGui.BeginTable("IRQTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Display");
                ImGui.TableSetupColumn("Timers");
                ImGui.TableSetupColumn("DMA");
                ImGui.TableSetupColumn("System");
                ImGui.TableHeadersRow();

                ImGui.PushFont(_monoFont);
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                RenderInterrupt(snapshot, 0, "VBlank");
                RenderInterrupt(snapshot, 1, "HBlank");
                RenderInterrupt(snapshot, 2, "VCounter");

                ImGui.TableSetColumnIndex(1);
                RenderInterrupt(snapshot, 3, "Timer0");
                RenderInterrupt(snapshot, 4, "Timer1");
                RenderInterrupt(snapshot, 5, "Timer2");
                RenderInterrupt(snapshot, 6, "Timer3");

                ImGui.TableSetColumnIndex(2);
                RenderInterrupt(snapshot, 8, "DMA0");
                RenderInterrupt(snapshot, 9, "DMA1");
                RenderInterrupt(snapshot, 10, "DMA2");
                RenderInterrupt(snapshot, 11, "DMA3");

                ImGui.TableSetColumnIndex(3);
                RenderInterrupt(snapshot, 7, "Serial");
                RenderInterrupt(snapshot, 12, "Keypad");
                RenderInterrupt(snapshot, 13, "GamePak");

                ImGui.EndTable();
                ImGui.PopFont();
            }

            ImGui.End();
        }


        private void RenderInterrupt(IRQSnapshot snapshot, int bit, ReadOnlySpan<char> label)
        {
            ushort mask = (ushort)(1 << bit);
            bool enabled = (snapshot.InterruptEnable & mask) != 0;
            bool pending = (snapshot.InterruptFlag & mask) != 0;

            Vector4 color;

            if (enabled && pending) color = _lavender;
            else if (enabled)       color = new Vector4(1f);
            else if (pending)       color = new Vector4(1.0f, 1.0f, 0.71f, 1f);
            else                    color = new Vector4(0.4f, 0.4f, 0.4f, 1f);

            ImGui.TextColored(color, label);
        }
    }
}