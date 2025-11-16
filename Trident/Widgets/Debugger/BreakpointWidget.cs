using ImGuiNET;
using System.Numerics;
using Trident.Widgets;
using Trident.Utilities;
using Trident.Core.Debugging.Breakpoints;


internal class BreakpointWidget(ImFontPtr monoFont, BreakpointManager breakpoints, Action<bool> pauseGBA) : IWidget
{
    private readonly ImFontPtr _monoFont = monoFont;
    private readonly BreakpointManager _breakpoints = breakpoints;
    private readonly Action<bool> _pauseGBA = pauseGBA;
    private readonly uint[] _bpBuffer = new uint[breakpoints.MaxBreakpoints];

    private int _selectedDeleteIndex = -1;
    private string _newBreakpointText = string.Empty;


    public bool IsVisible { get; set; } = true;

    public string Name => "Breakpoints";
    public string Group => "CPU";

    public void Render()
    {
        if (!IsVisible) return;

        if (!ImGui.Begin("Breakpoints"))
        {
            ImGui.End();
            return;
        }

        ImGui.PushFont(_monoFont);
        ImGui.InputTextWithHint("##bpAdd", "Address (hex)", ref _newBreakpointText, 16, ImGuiInputTextFlags.CharsHexadecimal);
        ImGui.PopFont();

        ImGui.SameLine();
        if (ImGui.Button("Add"))
        {
            if (uint.TryParse(_newBreakpointText,
                              System.Globalization.NumberStyles.HexNumber,
                              null,
                              out uint parsed))
            {
                if (_breakpoints.Add(parsed))
                    _newBreakpointText = string.Empty;
            }
        }

        int count = _breakpoints.CopyTo(_bpBuffer);
        if (_breakpoints.TryGetLastHit(out var hit))
        {
            for (int i = 0; i < count; i++)
            {
                if (_bpBuffer[i] == hit)
                {
                    _selectedDeleteIndex = i;
                    break;
                }
            }
        }

        if (_breakpoints.Enabled)
        {
            Span<char> addrBuf = stackalloc char[16];
            StackString addrStr = new(addrBuf);

            if (_selectedDeleteIndex >= 0 && _selectedDeleteIndex < count)
            {
                addrStr.Append("0x");
                addrStr.AppendFormatted(_bpBuffer[_selectedDeleteIndex], "X8");
            }
            else
            {
                addrStr.Append("<select>");
            }

            if (ImGui.BeginCombo("##bpDelete", addrStr.AsSpan()))
            {
                for (int i = 0; i < count; i++)
                {
                    bool isSelected = (_selectedDeleteIndex == i);

                    addrBuf.Clear();
                    addrStr.Reset();
                    addrStr.Append("0x");
                    addrStr.AppendFormatted(_bpBuffer[i], "X8");

                    if (ImGui.Selectable(addrStr.AsSpan(), isSelected))
                        _selectedDeleteIndex = i;

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (ImGui.Button("Delete") && _selectedDeleteIndex >= 0 && _selectedDeleteIndex < count)
            {
                _breakpoints.Remove(_bpBuffer[_selectedDeleteIndex]);

                if (_breakpoints.IsLastHit(_bpBuffer[_selectedDeleteIndex]))
                    _breakpoints.ClearLastHit();

                _selectedDeleteIndex = -1;
            }
        }
        else
            ImGui.TextDisabled("No breakpoints set");

        if (_breakpoints.TryGetLastHit(out var addr))
        {
            _pauseGBA(true);

            Span<char> bannerBuf = stackalloc char[32];
            StackString bannerStr = StackString.Interpolate(bannerBuf, $"Breakpoint hit at 0x{addr:X8}");
            ImGui.TextColored(new Vector4(1f, 0.8f, 0f, 1f), bannerStr.AsSpan());

            ImGui.SameLine();
            if (ImGui.SmallButton("Continue"))
            {
                _pauseGBA(false);
                _breakpoints.Continue(addr);
                _breakpoints.ClearLastHit();
            }
        }

        ImGui.End();
    }
}
