using ImGuiNET;
using System.Buffers;
using System.Numerics;
using System.Text.RegularExpressions;
using Trident.Core.Debugging.Disassembly;

using OperandToken = (System.ReadOnlyMemory<char> Text, Trident.Widgets.Debugger.OperandTokenType Type);

namespace Trident.Widgets.Debugger
{
    internal class DisassemblyWidget : IWidget
    {
        private readonly ImFontPtr _monoFont;

        private readonly Disassembler _disassembler;

        private readonly uint _currentInstructionHighlight = ImGui.ColorConvertFloat4ToU32(new(0.20f, 0.18f, 0.24f, 1.0f));
        private readonly Vector4 _colorAddress   = new(0.50f, 0.65f, 0.80f, 1.0f);
        private readonly Vector4 _colorOpcode    = new(0.70f, 0.85f, 0.90f, 1.0f);
        private readonly Vector4 _colorMnemonic  = new(0.55f, 0.95f, 0.85f, 1.0f);
        private readonly Vector4 _colorCondition = new(0.95f, 0.75f, 0.45f, 1.0f);
        private const float LeftMargin = 8f;

        private bool _showAddress = true;
        private bool _showOpcode = true;
        private bool _followPC = true;

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
                for (int i = 0; i < instructions.Count; i++)
                {
                    var instr = instructions[i];

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
                        ImGui.PushStyleColor(ImGuiCol.Text, _colorAddress);
                        ImGui.Text($"{instr.Address:X8}");
                        ImGui.PopStyleColor();
                        ImGui.SameLine(0f, 10f);
                    }

                    if (_showOpcode)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, _colorOpcode);
                        ImGui.Text(isThumb ? $"{instr.Opcode:X4}" : $"{instr.Opcode:X8}");
                        ImGui.PopStyleColor();
                        ImGui.SameLine(0f, 10f);
                    }


                    string mnemonic = instr.MnemonicBase;
                    string cond = instr.ConditionCode;

                    const int targetColumn = 8;
                    string padding = new(' ', Math.Max(0, targetColumn - (mnemonic.Length + cond.Length)));

                    ImGui.TextColored(_colorMnemonic, mnemonic);
                    ImGui.SameLine(0f, 0f);

                    ImGui.TextColored(_colorCondition, cond + padding);
                    ImGui.SameLine(0f, 0f);

                    for (int j = 0; j < instr.Operands.Count; j++)
                    {
                        string operand = instr.Operands[j];

                        using var tokens = OperandTokenizer.GetTokens(operand);
                        foreach (var tok in tokens.Span)
                        {
                            ImGui.TextColored(GetColorForToken(tok.Type), tok.Text.Span);
                            ImGui.SameLine(0f, 0f);
                        }

                        if (j < instr.Operands.Count - 1)
                        {
                            ImGui.TextColored(new Vector4(1f), ", ");
                            ImGui.SameLine(0.0f, 0.0f);
                        }
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


        private Vector4 GetColorForToken(OperandTokenType type) => type switch
        {
            OperandTokenType.Register       => new Vector4(0.65f, 0.80f, 1.00f, 1.0f),
            OperandTokenType.StatusRegister => new Vector4(0.50f, 0.90f, 0.60f, 1.0f),
            OperandTokenType.Immediate      => new Vector4(0.95f, 0.65f, 0.80f, 1.0f),
            OperandTokenType.ShiftType      => new Vector4(0.60f, 0.90f, 1.00f, 1.0f),
            OperandTokenType.Bracket        => new Vector4(1f),
            OperandTokenType.Comma          => new Vector4(1f),
            OperandTokenType.Symbol         => new Vector4(0.75f, 0.55f, 0.95f, 1.0f),
            OperandTokenType.Label          => new Vector4(0.90f, 0.80f, 0.55f, 1.0f),
            OperandTokenType.Unknown        => new Vector4(0.80f, 0.30f, 0.30f, 1.0f),
            _ => new Vector4(1f)
        };
    }


    internal static partial class OperandTokenizer
    {
        private static readonly Regex TokenRegex     = GenerateTokenRegex();
        private static readonly Regex RegisterRegex  = GenerateRegisterRegex(); 
        private static readonly Regex PSRRegex       = GeneratePSRRegex();
        private static readonly Regex ImmediateRegex = GenerateImmediateRegex();
        private static readonly Regex LabelRegex     = GenerateLabelRegex();

        [GeneratedRegex(@"(spsr(?:_[a-z]+)?|cpsr(?:_[a-z]+)?|lsl|lsr|asr|ror|rrx|r\d+|lr|sp|pc|#[-+]?(?:0x[0-9A-Fa-f]+|\d+)|0x[0-9A-Fa-f]+|[\[\]\{\},!^]|[-+]| )", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex GenerateTokenRegex();

        [GeneratedRegex(@"^(r\d+|lr|pc|sp)$", RegexOptions.Compiled)]
        private static partial Regex GenerateRegisterRegex();

        [GeneratedRegex(@"^(spsr|cpsr)(_[a-z]+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex GeneratePSRRegex();

        [GeneratedRegex(@"^#[-+]?(?:0x[0-9A-Fa-f]+|\d+)$", RegexOptions.Compiled)]
        private static partial Regex GenerateImmediateRegex();

        [GeneratedRegex(@"^0x[0-9A-Fa-f]+$", RegexOptions.Compiled)]
        private static partial Regex GenerateLabelRegex();


        internal static PooledTokens GetTokens(string operand)
        {
            OperandToken[] buffer = ArrayPool<OperandToken>.Shared.Rent(32);
            int count = FillTokens(operand, buffer);
            return new PooledTokens(buffer, count);
        }

        private static int FillTokens(string operand, OperandToken[] buffer)
        {
            int count = 0;
            ReadOnlyMemory<char> mem = operand.AsMemory();
            var matches = TokenRegex.Matches(operand);

            int lastIndex = 0;
            foreach (Match match in matches)
            {
                int index = match.Index;
                int length = match.Length;

                if (index > lastIndex)
                {
                    var between = mem.Slice(lastIndex, index - lastIndex).Trim();
                    if (!between.IsEmpty)
                        buffer[count++] = new OperandToken(between, OperandTokenType.Unknown);
                }

                var tokenMem = mem.Slice(index, length);
                buffer[count++] = new OperandToken(tokenMem, ClassifyToken(tokenMem.Span));
                lastIndex = index + length;
            }

            if (lastIndex < mem.Length)
            {
                var remaining = mem[lastIndex..].Trim();
                if (!remaining.IsEmpty)
                    buffer[count++] = new OperandToken(remaining, OperandTokenType.Unknown);
            }

            return count;
        }

        private static OperandTokenType ClassifyToken(ReadOnlySpan<char> token)
        {
            if (RegisterRegex.IsMatch(token)) return OperandTokenType.Register;
            if (ImmediateRegex.IsMatch(token)) return OperandTokenType.Immediate;

            if (token.Length == 1)
            {
                switch (token[0])
                {
                    case '[':
                    case ']':
                    case '{':
                    case '}':
                        return OperandTokenType.Bracket;
                    case ',':
                        return OperandTokenType.Comma;
                    case '!':
                    case '-':
                    case '+':
                    case '^':
                    case ' ':
                        return OperandTokenType.Symbol;
                }
            }

            if (token.Equals("lsl".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                token.Equals("lsr".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                token.Equals("asr".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                token.Equals("ror".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                token.Equals("rrx".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return OperandTokenType.ShiftType;
            }

            if (LabelRegex.IsMatch(token)) return OperandTokenType.Label;
            if (PSRRegex.IsMatch(token)) return OperandTokenType.StatusRegister;

            return OperandTokenType.Unknown;
        }
    }

    internal sealed class PooledTokens : IDisposable
    {
        private OperandToken[]? _buffer;
        internal int Count { get; }
        internal ReadOnlySpan<OperandToken> Span => _buffer.AsSpan(0, Count);

        internal PooledTokens(OperandToken[] buffer, int count)
        {
            _buffer = buffer;
            Count = count;
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                ArrayPool<OperandToken>.Shared.Return(_buffer, clearArray: true);
                _buffer = null;
            }
        }
    }


    enum OperandTokenType
    {
        Register,
        StatusRegister,
        Immediate,
        ShiftType,
        Bracket,
        Comma,
        Symbol,
        Label,
        Unknown
    }
}