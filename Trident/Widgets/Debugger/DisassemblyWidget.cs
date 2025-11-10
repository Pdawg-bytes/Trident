using ImGuiNET;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using Trident.Core.Debugging.Disassembly;
using Trident.Core.Debugging.Disassembly.Tokens;
using Trident.Utilities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Trident.Widgets.Debugger
{
    internal class DisassemblyWidget : IWidget
    {
        private readonly ImFontPtr _monoFont;

        private readonly Disassembler _disassembler;

        private readonly uint _currentInstructionHighlight = ImGui.ColorConvertFloat4ToU32(new(0.20f, 0.18f, 0.24f, 1.0f));
        private readonly Vector4 _colorAddress = new(0.50f, 0.65f, 0.80f, 1.0f);
        private readonly Vector4 _colorOpcode = new(0.70f, 0.85f, 0.90f, 1.0f);
        private readonly Vector4 _colorCondition = new(0.95f, 0.75f, 0.45f, 1.0f);
        private const float LeftMargin = 8f;

        private bool _showAddress = true;
        private bool _showOpcode = true;
        private bool _followPC = true;

        internal readonly static string[] _registers = ["r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12", "sp", "lr", "pc"];

        internal DisassemblyWidget(ImFontPtr monoFont, Disassembler disassembler)
        {
            _monoFont = monoFont;
            _disassembler = disassembler;
        }

        public bool IsVisible { get; set; } = true;

        public string Name => "Disassembly";
        public string Group => "CPU";

        public void Render()
        {
            if (!IsVisible) return;

            if (!ImGui.Begin("Disassembly"))
            {
                ImGui.End();
                return;
            }


            ImGui.Checkbox("Show Address", ref _showAddress);
            ImGui.SameLine();
            ImGui.Checkbox("Show Bytecode", ref _showOpcode);
            ImGui.SameLine();
            ImGui.Checkbox("Follow PC", ref _followPC);

            ImGui.Separator();

            ImGui.BeginChild("DisasmScroll");

            if (ImGui.BeginTable("DisasmTable", 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.PushFont(_monoFont);

                int currentRowIndex = -1;
                var (actualAddress, isThumb, instructions) = _disassembler.GetAroundPC(30, 30);
                var instructionsSpan = instructions.Span;

                Span<char> addrBuf = stackalloc char[16];
                Span<char> opBuf = stackalloc char[16];

                for (int i = 0; i < instructions.Length; i++)
                {
                    var instr = instructionsSpan[i];

                    ImGui.TableNextRow();

                    if (instr.Address == actualAddress)
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, _currentInstructionHighlight);
                        currentRowIndex = i;
                    }

                    ImGui.TableNextColumn();

                    ImGui.Indent(LeftMargin);

                    if (_showAddress)
                    {
                        addrBuf.Clear();

                        var addrStr = StackString.Interpolate(addrBuf, $"{instr.Address:X8}");

                        ImGui.PushStyleColor(ImGuiCol.Text, _colorAddress);
                        ImGui.Text(addrStr.AsSpan());
                        ImGui.PopStyleColor();
                        ImGui.SameLine(0f, 10f);
                    }

                    if (_showOpcode)
                    {
                        opBuf.Clear();

                        var opStr = isThumb
                            ? StackString.Interpolate(opBuf, $"{instr.Opcode:X4}")
                            : StackString.Interpolate(opBuf, $"{instr.Opcode:X8}");

                        ImGui.PushStyleColor(ImGuiCol.Text, _colorOpcode);
                        ImGui.Text(opStr.AsSpan());
                        ImGui.PopStyleColor();
                        ImGui.SameLine(0f, 10f);
                    }

                    int mnemonicChars = 0;
                    var reader = new TokenReader(instr.Tokens.Span[..instr.OperandsStartIndex]);
                    while (reader.TryRead(out var tok))
                    {
                        mnemonicChars += tok.Type switch
                        {
                            TokenType.Mnemonic => tok.Length,
                            TokenType.Syntax => 1,
                            _ => tok.Data.Length,
                        };
                    }

                    const int targetColumn = 8;
                    int padding = Math.Max(0, targetColumn - mnemonicChars);

                    reader = new TokenReader(instr.Tokens.Span[..instr.OperandsStartIndex]);
                    while (reader.TryRead(out var tok))
                    {
                        Vector4 color;

                        if (tok.Type == TokenType.Mnemonic && tok.Data[0] == 1)
                            color = _colorCondition;
                        else
                            color = GetColorForToken(tok.Type);

                        ImGui.TextColored(color, DecodeTokenText(tok));
                        ImGui.SameLine(0f, 0f);

                        if (reader.Remaining == 0 && padding > 0)
                        {
                            ImGui.TextUnformatted(new string(' ', padding));
                            ImGui.SameLine(0f, 0f);
                        }
                    }

                    reader = new TokenReader(instr.Tokens.Span[instr.OperandsStartIndex..]);
                    while (reader.TryRead(out var tok))
                    {
                        var color = GetColorForToken(tok.Type);
                        ImGui.TextColored(color, DecodeTokenText(tok));
                        ImGui.SameLine(0f, 0f);
                    }

                    ImGui.Unindent(LeftMargin);
                }

                if (_followPC && currentRowIndex >= 0)
                {
                    float scrollWindowHeight = ImGui.GetWindowHeight();
                    float rowHeight = ImGui.GetTextLineHeightWithSpacing() + (2.2f * ImGui.GetWindowDpiScale());
                    float scrollTarget = rowHeight * currentRowIndex - scrollWindowHeight / 2 + rowHeight / 2;
                    scrollTarget = Math.Clamp(scrollTarget, 0, ImGui.GetScrollMaxY());
                    ImGui.SetScrollY(scrollTarget);
                }

                ImGui.EndTable();
                ImGui.PopFont();
            }

            ImGui.EndChild();
            ImGui.End();
        }


        private Vector4 GetColorForToken(TokenType type) => type switch
        {
            TokenType.Register => new Vector4(0.65f, 0.80f, 1.00f, 1.0f),
            TokenType.PSR      => new Vector4(0.50f, 0.90f, 0.60f, 1.0f),
            TokenType.Number   => new Vector4(0.95f, 0.65f, 0.80f, 1.0f),
            TokenType.Mnemonic => new Vector4(0.55f, 0.95f, 0.85f, 1.0f),
            TokenType.Syntax   => new Vector4(1f),
            //TokenType.Label  => new Vector4(0.90f, 0.80f, 0.55f, 1.0f),
            TokenType.Unknown  => new Vector4(0.80f, 0.30f, 0.30f, 1.0f),
            _ => new Vector4(1f)
        };


        string DecodeTokenText(Token tok)
        {
            switch (tok.Type)
            {
                case TokenType.Mnemonic:
                    {
                        bool cond = tok.Data[0] != 0;
                        return Encoding.ASCII.GetString(tok.Data.Slice(1, tok.Length));
                    }

                case TokenType.Register: return _registers[tok.Data[0]];

                case TokenType.Number:
                    byte flag = tok.Data[0];
                    bool neg = (flag & 1) != 0;
                    bool lbl = (flag & 2) != 0;
                    uint val = BinaryPrimitives.ReadUInt32LittleEndian(tok.Data.Slice(1, 4));
                    return $"{(lbl ? "" : "#")}{(neg ? "-" : "")}0x{val:X}";

                case TokenType.PSR:
                    byte b = tok.Data[0];
                    bool cpsr = (b & 0x80) != 0;
                    PSRFlags flags = (PSRFlags)(b & 0x7F);
                    return flags == PSRFlags.None
                        ? (cpsr ? "cpsr" : "spsr")
                        : $"{(cpsr ? "cpsr" : "spsr")}_{FormatFlags(flags)}";

                case TokenType.Coprocessor:
                    {
                        byte f = tok.Data[0];
                        bool isReg = (f & 0x80) != 0;
                        byte idx = (byte)(f & 0x0F);
                        return $"{(isReg ? 'c' : 'p')}{idx}";
                    }

                case TokenType.Syntax:
                    return ((char)tok.Data[0]).ToString();

                default: return Encoding.ASCII.GetString(tok.Data);
            }
        }

        string FormatFlags(PSRFlags flags)
        {
            const string flagStr = "fsxc";
            var sb = new StringBuilder();
            for (int j = 0; j < 4; j++)
                if (((int)flags & (1 << j)) != 0)
                    sb.Append(flagStr[j]);
            return sb.ToString();
        }
    }
}