using ImGuiNET;
using System.Numerics;
using Trident.Utilities;
using Trident.Core.Hardware.DMA;
using Trident.Core.Debugging.Snapshots;

namespace Trident.Widgets.Debugger
{
    internal class DMAControllerWidget(ImFontPtr monoFont, Func<DMASnapshot> getSnapshot) : IWidget
    {
        private readonly Func<DMASnapshot> _getSnapshot = getSnapshot;

        private readonly ImFontPtr _monoFont = monoFont;

        private readonly Vector4 _lavender = new(0.87f, 0.82f, 0.97f, 1f);
        private readonly uint _tableHighlight = ImGui.ColorConvertFloat4ToU32(new(0.25f, 0.11f, 0.43f, 0.50f));

        private static readonly string[] _headers = [ "DMA Channel 0", "DMA Channel 1", "DMA Channel 2", "DMA Channel 3" ];


        public bool IsVisible { get; set; } = true;

        public string Name => "DMA Controller";
        public string Group => "System";

        public void Render()
        {
            if (!IsVisible) return;

            if (!ImGui.Begin("DMA"))
            {
                ImGui.End();
                return;
            }


            DMASnapshot snapshot = _getSnapshot();

            Span<DMASnapshot.ChannelSnapshot> channels = 
            [ 
                snapshot.Channel0, 
                snapshot.Channel1, 
                snapshot.Channel2, 
                snapshot.Channel3
            ];


            Span<char> numBuf = stackalloc char[12];
            StackString numStr = new(numBuf);

            Span<char> headerBuf = stackalloc char[20];

            for (int i = 0; i < channels.Length; i++)
            {
                var ch = channels[i];

                if (ImGui.CollapsingHeader(_headers[i]))
                {
                    if (ImGui.BeginTable($"##dmach", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                    {
                        ImGui.TableSetupColumn("Addresses");
                        ImGui.TableSetupColumn("Controls");
                        ImGui.TableHeadersRow();

                        ImGui.TableNextRow();

                        ImGui.PushFont(_monoFont);
                        ImGui.TableSetColumnIndex(0);

                        ImGui.TextDisabled("SRC "); ImGui.SameLine();
                        numStr = StackString.Interpolate(numBuf, $"0x{ch.Source:X8}");
                        ImGui.TextUnformatted(numStr.AsSpan());

                        ImGui.TextDisabled("DST "); ImGui.SameLine();
                        numStr = StackString.Interpolate(numBuf, $"0x{ch.Destination:X8}");
                        ImGui.TextUnformatted(numStr.AsSpan());

                        ImGui.TextDisabled("LEN "); ImGui.SameLine();
                        numStr = StackString.Interpolate(numBuf, $"0x{ch.TransferLength:X4}");
                        ImGui.TextUnformatted(numStr.AsSpan());


                        ImGui.TableSetColumnIndex(1);
                        ImGui.TextDisabled("SIZ "); ImGui.SameLine();
                        ImGui.Text(TransferSizeString(ch.TransferSize));

                        ImGui.TextDisabled("SRC "); ImGui.SameLine();
                        ImGui.TextUnformatted(AddressingModeString(ch.SourceControl));

                        ImGui.TextDisabled("DST "); ImGui.SameLine();
                        ImGui.Text(AddressingModeString(ch.DestinationControl));

                        ImGui.TextDisabled("TIM "); ImGui.SameLine();
                        ImGui.Text(StartTimingString(ch.StartTiming));
                        ImGui.PopFont();

                        ImGui.EndTable();
                    }

                    if (ImGui.BeginTable($"##dmaflags", (i == 3) ? 4 : 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                    {
                        ImGui.TableNextRow();

                        ImGui.PushFont(_monoFont);
                        ImGui.TableSetColumnIndex(0); RenderFlagCell("Enable", ch.Enabled);
                        ImGui.TableSetColumnIndex(1); RenderFlagCell("Repeat", ch.Repeat);
                        ImGui.TableSetColumnIndex(2); RenderFlagCell("Interrupt", ch.InterruptOnEnd);
                        if (i == 3) { ImGui.TableSetColumnIndex(3); RenderFlagCell("DRQ", ch.GamePakDRQ); }
                        ImGui.PopFont();

                        ImGui.EndTable();
                    }
                }
            }

            ImGui.End();
        }


        private void RenderFlagCell(ReadOnlySpan<char> label, bool state)
        {
            if (state)
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, _tableHighlight);

            Vector2 cellSize = ImGui.GetContentRegionAvail();
            Vector2 textSize = ImGui.CalcTextSize(label);
            float xOffset = (cellSize.X - textSize.X) * 0.5f;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + xOffset);

            ImGui.TextUnformatted(label);
        }


        private ReadOnlySpan<char> AddressingModeString(AddressingMode mode) => mode switch
        {
            AddressingMode.Increment => "Increment",
            AddressingMode.Decrement => "Decrement",
            AddressingMode.Fixed     => "Fixed",
            AddressingMode.Reload    => "Reload",
            _ => "UNKNOWN"
        };

        private ReadOnlySpan<char> StartTimingString(DMAStartTiming timing) => timing switch
        {
            DMAStartTiming.Immediate => "Immediate",
            DMAStartTiming.VBlank    => "VBlank",
            DMAStartTiming.HBlank    => "HBlank",
            DMAStartTiming.Special   => "Special",
            _ => "UNKNOWN"
        };

        private ReadOnlySpan<char> TransferSizeString(DMATransferSize size) => size switch
        {
            DMATransferSize.Half => "Halfword",
            DMATransferSize.Word => "Word",
            _ => "UNKNOWN"
        };
    }
}