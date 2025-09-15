using ImGuiNET;
using System.Numerics;
using System.Text.RegularExpressions;
using Trident.Core.Debugging.Disassembly;

using OperandToken = (string Text, Trident.Widgets.Debugger.OperandTokenType Type);

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
        private const float LEFT_MARGIN = 8f;

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

            if (ImGui.Begin("Disassembly"))
            {
                ImGui.Checkbox("Show Address", ref _showAddress);
                ImGui.SameLine(); 
                ImGui.Checkbox("Show Bytecode", ref _showOpcode);

                ImGui.Checkbox("Follow PC", ref _followPC);

                ImGui.Separator();

                ImGui.BeginChild("DisasmScroll");

                if (ImGui.BeginTable("DisasmTable", 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
                {
                    ImGui.PushFont(_monoFont);

                    int currentRowIndex = -1;
                    var (actualAddress, instructions) = _disassembler.GetAroundPC(20, 20);
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

                        ImGui.Indent(LEFT_MARGIN);

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
                            ImGui.Text($"{instr.Opcode:X8}");
                            ImGui.PopStyleColor();
                            ImGui.SameLine(0f, 10f);
                        }


                        string mnemonic = instr.MnemonicBase;
                        string cond = instr.ConditionCode;

                        const int targetColumn = 8;
                        string padding = new string(' ', Math.Max(0, targetColumn - (mnemonic.Length + cond.Length)));

                        ImGui.TextColored(_colorMnemonic, mnemonic);
                        ImGui.SameLine(0f, 0f);

                        ImGui.TextColored(_colorCondition, cond + padding);
                        ImGui.SameLine(0f, 0f);

                        for (int j = 0; j < instr.Operands.Count; j++)
                        {
                            string operand = instr.Operands[j];

                            foreach (var token in OperandTokenizer.GetTokens(operand))
                            {
                                ImGui.TextColored(GetColorForToken(token.Type), token.Text);
                                ImGui.SameLine(0f, 0f);
                            }

                            if (j < instr.Operands.Count - 1)
                            {
                                ImGui.TextColored(new Vector4(1f), ", ");
                                ImGui.SameLine(0.0f, 0.0f);
                            }
                        }

                        ImGui.Unindent(LEFT_MARGIN);
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
            }

            ImGui.End();
        }


        private Vector4 GetColorForToken(OperandTokenType type) => type switch
        {
            OperandTokenType.Register  => new Vector4(0.65f, 0.80f, 1.00f, 1.0f),
            OperandTokenType.Immediate => new Vector4(0.95f, 0.65f, 0.80f, 1.0f),
            OperandTokenType.Bracket   => new Vector4(1f),
            OperandTokenType.Comma     => new Vector4(1f),
            OperandTokenType.Symbol    => new Vector4(0.75f, 0.55f, 0.95f, 1.0f),
            OperandTokenType.Label     => new Vector4(0.90f, 0.80f, 0.55f, 1.0f),
            OperandTokenType.Unknown   => new Vector4(0.80f, 0.30f, 0.30f, 1.0f),
            _ => new Vector4(1f)
        };
    }


    internal static partial class OperandTokenizer
    {
        private static readonly Dictionary<string, int> _operandUsageCount = new();
        private static readonly Dictionary<string, List<OperandToken>> _tokenCache = new();

        private const int UsageThreshold = 3;
        private const int MaxCacheSize = 1024;
        private static readonly Random _random = new();


        private static readonly Regex TokenRegex     = GenerateTokenRegex();
        private static readonly Regex RegisterRegex  = GenerateRegisterRegex();
        private static readonly Regex ImmediateRegex = GenerateImmediateRegex();
        private static readonly Regex LabelRegex     = GenerateLabelRegex();

        [GeneratedRegex(@"(r\d+|lr|sp|pc|#(?:0x)?[0-9A-Fa-f]+|0x[0-9A-Fa-f]+|[\[\]\{\},!^]|[-+]| )", RegexOptions.Compiled)]
        private static partial Regex GenerateTokenRegex();

        [GeneratedRegex(@"^r\d+$|^lr$|^pc$|^sp$", RegexOptions.Compiled)]
        private static partial Regex GenerateRegisterRegex();

        [GeneratedRegex(@"^#(?:0x)?[0-9A-Fa-f]+$", RegexOptions.Compiled)]
        private static partial Regex GenerateImmediateRegex();

        [GeneratedRegex(@"^0x[0-9A-Fa-f]+$", RegexOptions.Compiled)]
        private static partial Regex GenerateLabelRegex();


        internal static List<OperandToken> GetTokens(string operand)
        {
            if (_tokenCache.TryGetValue(operand, out var cached))
                return cached;

            if (_operandUsageCount.TryGetValue(operand, out int count))
            {
                count++;
                _operandUsageCount[operand] = count;

                if (count >= UsageThreshold)
                {
                    var tokens = TokenizeOperand(operand);

                    if (_tokenCache.Count >= MaxCacheSize)
                    {
                        string randomKey = _tokenCache.Keys.ElementAt(_random.Next(_tokenCache.Count));
                        _tokenCache.Remove(randomKey);
                    }

                    _tokenCache[operand] = tokens;
                    return tokens;
                }
            }
            else
                _operandUsageCount[operand] = 1;

            return TokenizeOperand(operand);
        }

        private static List<OperandToken> TokenizeOperand(string operand)
        {
            List<OperandToken> tokens = [];
            ReadOnlySpan<char> span = operand.AsSpan();
            var matches = TokenRegex.Matches(operand);

            int lastIndex = 0;
            foreach (Match match in matches)
            {
                int index = match.Index;
                int length = match.Length;

                if (index > lastIndex)
                {
                    var betweenSpan = span.Slice(lastIndex, index - lastIndex).Trim();
                    if (!betweenSpan.IsEmpty)
                        tokens.Add((betweenSpan.ToString(), OperandTokenType.Unknown));
                }

                var tokenSpan = span.Slice(index, length);
                tokens.Add((tokenSpan.ToString(), ClassifyToken(tokenSpan.ToString())));
                lastIndex = index + length;
            }

            if (lastIndex < span.Length)
            {
                var remainingSpan = span[lastIndex..].Trim();
                if (!remainingSpan.IsEmpty)
                    tokens.Add((remainingSpan.ToString(), OperandTokenType.Unknown));
            }

            return tokens;
        }

        private static OperandTokenType ClassifyToken(string token) => token switch
        {
            _ when RegisterRegex.IsMatch(token)  => OperandTokenType.Register,
            _ when ImmediateRegex.IsMatch(token) => OperandTokenType.Immediate,
            _ when "[]{}".Contains(token)        => OperandTokenType.Bracket,
            ","                                  => OperandTokenType.Comma,
            "!" or "-" or "+" or "^" or " "      => OperandTokenType.Symbol,
            _ when LabelRegex.IsMatch(token)     => OperandTokenType.Label,
            _                                    => OperandTokenType.Unknown
        };
    }


    enum OperandTokenType
    {
        Register,
        Immediate,
        Bracket,
        Comma,
        Symbol,
        Label,
        Unknown
    }
}