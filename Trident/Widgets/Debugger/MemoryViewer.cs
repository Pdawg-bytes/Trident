using ImGuiNET;
using System.Numerics;
using Trident.Utilities;
using Trident.Core.Memory;

namespace Trident.Widgets.Debugger
{
    internal class MemoryViewer : IWidget
    {
        private Func<uint, DebugMemoryRead<byte>> _readFunc;

        private string _gotoAddressInput = "";
        private uint _gotoAddress;
        private bool _gotoRequested;

        private bool _regionChanged = false;
        private uint _baseAddress = 0x0000;
        private int _selectedRegionIndex = 0;
        private readonly (string Name, uint BaseAddress)[] _regions =
        {
            ("BIOS",    0x00000000),
            ("EWRAM",   0x02000000),
            ("IWRAM",   0x03000000),
            ("PRAM",    0x05000000),
            ("VRAM",    0x06000000),
            ("OAM",     0x07000000),
            ("GamePak", 0x08000000)
        };

        private readonly ImFontPtr _monoFont;

        private readonly Vector4 _addressColor = new(0.7f, 0.7f, 0.7f, 1f);

        private const uint BytesPerRow = 16;
        private bool _showAscii = true;
        private readonly DebugMemoryRead<byte>[] _rowBytes = new DebugMemoryRead<byte>[BytesPerRow];
        internal static readonly string[] _columnHeaders = Enumerable.Range(0, 16).Select(i => i.ToString("X2")).ToArray();

        internal MemoryViewer(ImFontPtr monoFont)
        {
            _monoFont = monoFont;
        }

        public bool IsVisible { get; set; } = true;

        public string Name => "Memory Viewer";
        public string Group => "Memory";

        public void Render()
        {
            if (!IsVisible || _readFunc == null)
                return;

            if (!ImGui.Begin("Memory Viewer", ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.End();
                return;
            }

            ImGui.Dummy(new(0));

            ImGui.TextUnformatted("Region  ");
            ImGui.SameLine();
            if (ImGui.BeginCombo("##regionCombo", _regions[_selectedRegionIndex].Name))
            {
                for (int i = 0; i < _regions.Length; i++)
                {
                    bool isSelected = (i == _selectedRegionIndex);
                    if (ImGui.Selectable(_regions[i].Name, isSelected))
                    {
                        _selectedRegionIndex = i;
                        _baseAddress = _regions[i].BaseAddress;
                        _regionChanged = true;
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            ImGui.Checkbox("Show ASCII", ref _showAscii);

            ImGui.TextUnformatted("Address");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(140);
            if (ImGui.InputText("##gotoAddress", ref _gotoAddressInput, 16, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsHexadecimal))
            {
                if (uint.TryParse(_gotoAddressInput, System.Globalization.NumberStyles.HexNumber, null, out var parsed))
                {
                    _gotoAddress = parsed;
                    _gotoRequested = true;
                }
            }

            ImGui.Separator();


            ImGui.BeginChild("MemoryViewerHeader", new Vector2(800, ImGui.GetFontSize()));
            ImGui.PushFont(_monoFont);
            ImGui.TextUnformatted("         ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, _addressColor);
            for (uint col = 0; col < 16; col++)
            {
                ImGui.SameLine();
                ImGui.TextUnformatted(_columnHeaders[col]);
            }
            ImGui.PopStyleColor();
            ImGui.PopFont();
            ImGui.EndChild();


            ImGui.BeginChild("MemoryViewerRegion");

            const uint bytesPerRow = 16;
            var regionInfo = _readFunc(_baseAddress);
            if (!regionInfo.IsValid)
            {
                ImGui.TextUnformatted("Invalid memory region.");
                ImGui.EndChild();
                ImGui.End();
                return;
            }


            uint regionStart = regionInfo.BaseAddress;
            uint regionEnd   = regionInfo.EndAddress;
            uint totalBytes  = regionEnd - regionStart;
            uint totalRows   = totalBytes / bytesPerRow;

            float rowHeight   = ImGui.GetFontSize() + ImGui.GetStyle().ItemSpacing.Y;
            ImGui.Dummy(new Vector2(1, rowHeight * totalRows));


            uint firstVisibleRow;
            if (_regionChanged)
            {
                ImGui.SetScrollY(0);
                _regionChanged = false;
                firstVisibleRow = 0;
            }
            else
                firstVisibleRow = (uint)(ImGui.GetScrollY() / rowHeight);

            uint visibleRowCount = (uint)(ImGui.GetWindowHeight() / rowHeight) + 1;
            uint lastVisibleRow  = Math.Min(firstVisibleRow + visibleRowCount, totalRows);

            ImGui.SetCursorPosY(firstVisibleRow * rowHeight);
            ImGui.PushFont(_monoFont);

            if (_gotoRequested && _gotoAddress >= regionStart && _gotoAddress < regionEnd)
            {
                uint rowIndex = (_gotoAddress - regionStart) / bytesPerRow;
                ImGui.SetScrollY(rowIndex * rowHeight);
                _gotoRequested = false;
            }


            Span<char> addrBuf  = stackalloc char[10];
            Span<char> hexBuf   = stackalloc char[4];
            Span<char> asciiBuf = stackalloc char[(int)bytesPerRow + 2];

            for (uint row = firstVisibleRow; row < lastVisibleRow; row++)
            {
                uint addr = regionStart + row * bytesPerRow;

                ImGui.PushStyleColor(ImGuiCol.Text, _addressColor);
                var addrStr = StackString.Interpolate(addrBuf, $"{addr:X8} ");
                ImGui.TextUnformatted(addrStr.AsSpan());
                ImGui.PopStyleColor();
                ImGui.SameLine();

                for (uint col = 0; col < bytesPerRow; col++)
                {
                    uint byteAddr = addr + col;
                    var result = _rowBytes[col] = _readFunc(byteAddr);

                    ImGui.SameLine();
                    if (result.IsValid)
                    {
                        var hexStr = StackString.Interpolate(hexBuf, $"{result.Value:X2}");
                        ImGui.TextUnformatted(hexStr.AsSpan());
                    }
                }

                if (_showAscii)
                {
                    var asciiStr = new StackString(asciiBuf);
                    for (int col = 0; col < bytesPerRow; col++)
                    {
                        var result = _rowBytes[col];
                        asciiStr += result.IsValid && result.Value >= 32 && result.Value <= 126
                            ? (char)result.Value
                            : '.';
                    }
                    ImGui.SameLine(450);
                    ImGui.TextUnformatted(asciiStr.AsSpan());
                }
            }

            ImGui.PopFont();
            ImGui.EndChild();
            ImGui.End();
        }


        internal void SetReadFunction(Func<uint, DebugMemoryRead<byte>> func) => _readFunc = func;
    }
}