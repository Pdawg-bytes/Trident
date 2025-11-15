using ImGuiNET;
using System.Numerics;
using Trident.Utilities;
using System.Buffers.Binary;
using Trident.Core.Debugging.Disassembly;
using Trident.Core.Debugging.Disassembly.Tokens;

namespace Trident.Widgets.Debugger
{
    internal class DisassemblyWidget(ImFontPtr monoFont, Disassembler disassembler) : IWidget
    {
        private readonly ImFontPtr _monoFont = monoFont;

        private readonly Disassembler _disassembler = disassembler;

        private readonly uint _currentInstructionHighlight = ImGui.ColorConvertFloat4ToU32(new(0.20f, 0.18f, 0.24f, 1.0f));
        private readonly Vector4 _colorAddress = new(0.50f, 0.65f, 0.80f, 1.0f);
        private readonly Vector4 _colorOpcode = new(0.70f, 0.85f, 0.90f, 1.0f);
        private readonly Vector4 _colorCondition = new(0.95f, 0.75f, 0.45f, 1.0f);
        private const float LeftMargin = 8f;

        private bool _showAddress = true;
        private bool _showOpcode = true;
        private bool _followPC = true;

        internal readonly static string[] _registers = ["r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12", "sp", "lr", "pc"];


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

                Span<char> disasmBuffer = stackalloc char[64];
                Span<char> padBuffer = stackalloc char[8];
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


                    StackString text = new(disasmBuffer);
                    StackString pad = new(padBuffer);

                    int mnemonicChars = 0;
                    const int targetColumn = 8;

                    var reader = new TokenReader(instr.Tokens.Span);
                    while (!reader.EndOfStream)
                    {
                        if (!reader.TryRead(out var token, out int tokenOffset))
                            break;

                        bool isOperand = tokenOffset >= instr.OperandsStartIndex;

                        if (!isOperand)
                        {
                            mnemonicChars += token.Type switch
                            {
                                TokenType.Mnemonic => token.Length,
                                TokenType.Syntax   => 1,
                                _                  => token.Data.Length,
                            };
                        }

                        text = new(disasmBuffer);
                        DecodeTokenText(token, ref text);
                        ImGui.TextColored(GetColorForToken(token), text.AsSpan());
                        ImGui.SameLine(0f, 0f);

                        if (!isOperand && tokenOffset + 1 + token.Data.Length >= instr.OperandsStartIndex && mnemonicChars < targetColumn)
                        {
                            pad = new(padBuffer);
                            pad.Repeat(' ', targetColumn - mnemonicChars);
                            ImGui.TextUnformatted(pad.AsSpan());
                            ImGui.SameLine(0f, 0f);
                        }
                    }

                    ImGui.Unindent(LeftMargin);
                }

                if (_followPC && currentRowIndex >= 0)
                {
                    float scrollWindowHeight = ImGui.GetWindowHeight() + 12;
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


        private Vector4 GetColorForToken(Token token)
        {
            if (token.Type == TokenType.Number && (token.Data[0] & 2) != 0)
                return new Vector4(0.90f, 0.80f, 0.55f, 1.0f);

            if (token.Type == TokenType.Mnemonic && token.Data[0] == 1)
                return _colorCondition;

            return token.Type switch
            {
                TokenType.Register => new Vector4(0.65f, 0.80f, 1.00f, 1.0f),
                TokenType.PSR      => new Vector4(0.50f, 0.90f, 0.60f, 1.0f),
                TokenType.Number   => new Vector4(0.95f, 0.65f, 0.80f, 1.0f),
                TokenType.Mnemonic => new Vector4(0.55f, 0.95f, 0.85f, 1.0f),
                TokenType.Syntax   => new Vector4(1f),
                TokenType.Unknown  => new Vector4(0.80f, 0.30f, 0.30f, 1.0f),
                _                  => new Vector4(0.80f, 0.30f, 0.30f, 1.0f)
            };
        }


        private void DecodeTokenText(Token token, ref StackString output)
        {
            switch (token.Type)
            {
                case TokenType.Mnemonic:
                    output.Append(token.Data.Slice(1, token.Length));
                    break;

                case TokenType.Register:
                    output.Append(_registers[token.Data[0]]);
                    break;

                case TokenType.Number:
                    {
                        byte flag = token.Data[0];
                        bool neg = (flag & 1) != 0;
                        bool lbl = (flag & 2) != 0;
                        bool hex = (flag & 4) != 0;
                        uint val = BinaryPrimitives.ReadUInt32LittleEndian(token.Data.Slice(1, 4));

                        if (!lbl) output.Append('#');
                        if (neg) output.Append('-');
                        if (hex) output.Append("0x");
                        output.AppendFormatted(val, hex ? (lbl ? "X8" : "X") : "");
                    }
                    break;

                case TokenType.PSR:
                    {
                        byte flag = token.Data[0];
                        bool cpsr = (flag & 0x80) != 0;
                        PSRFlags flags = (PSRFlags)(flag & 0x7F);

                        output.Append(cpsr ? "cpsr" : "spsr");

                        if (flags != PSRFlags.None)
                        {
                            output.Append('_');

                            const string flagStr = "fsxc";

                            for (int i = 0; i < 4; i++)
                                if (((int)flags & (1 << i)) != 0)
                                    output.Append(flagStr[i]);
                        }
                    }
                    break;

                case TokenType.Coprocessor:
                    byte f = token.Data[0];
                    bool isReg = (f & 0x80) != 0;
                    byte idx = (byte)(f & 0x0F);
                    output.Append(isReg ? 'c' : 'p');
                    output.AppendFormatted(idx);
                    break;

                case TokenType.Syntax:
                    output.Append((char)token.Data[0]);
                    break;

                default:
                    output.Append(token.Data);
                    break;
            }
        }
    }
}