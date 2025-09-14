using ImGuiNET;
using System.Numerics;
using System.Text.RegularExpressions;

using OperandToken = (string Text, Trident.Widgets.Debugger.OperandTokenType Type);

namespace Trident.Widgets.Debugger
{
    internal class DisassemblyWidget : IWidget
    {
        private readonly ImFontPtr _monoFont;

        private readonly uint _currentInstructionHighlight = ImGui.ColorConvertFloat4ToU32(new(0.20f, 0.18f, 0.24f, 1.0f));
        private readonly Vector4 _colorAddress   = new(0.50f, 0.65f, 0.80f, 1.0f);
        private readonly Vector4 _colorOpcode    = new(0.70f, 0.85f, 0.90f, 1.0f);
        private readonly Vector4 _colorMnemonic  = new(0.55f, 0.95f, 0.85f, 1.0f);
        private readonly Vector4 _colorCondition = new(0.95f, 0.75f, 0.45f, 1.0f);
        private readonly Vector4 _colorFlags     = new(1.00f, 0.55f, 0.75f, 1.0f);

        private bool _showAddress = true;
        private bool _showOpcode = true;

        internal DisassemblyWidget(ImFontPtr monoFont)
        {
            _monoFont = monoFont;
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

                ImGui.Separator();

                ImGui.BeginChild("DisasmScroll");

                if (ImGui.BeginTable("DisasmTable", 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
                {
                    ImGui.PushFont(_monoFont);

                    bool firstInstr = true;
                    foreach (var instr in _mockDisasm)
                    {
                        ImGui.TableNextRow();

                        if (firstInstr)
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, _currentInstructionHighlight);
                            firstInstr = false;
                        }

                        ImGui.TableNextColumn();

                        if (_showAddress)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, _colorAddress);
                            ImGui.Text($"{instr.Address:X8} ");
                            ImGui.PopStyleColor();
                            ImGui.SameLine();
                        }

                        if (_showOpcode)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, _colorOpcode);
                            ImGui.Text($"{instr.Opcode:X8} ");
                            ImGui.PopStyleColor();
                            ImGui.SameLine();
                        }


                        string mnemonic = instr.MnemonicBase;
                        string cond = instr.ConditionCode;

                        const int targetColumn = 8;
                        string padding = new string(' ', Math.Max(0, targetColumn - (mnemonic.Length + cond.Length + (instr.SetsFlags ? 1 : 0))));

                        ImGui.TextColored(_colorMnemonic, mnemonic);
                        ImGui.SameLine(0f, 0f);

                        ImGui.TextColored(_colorCondition, cond);
                        ImGui.SameLine(0f, 0f);

                        ImGui.TextColored(_colorFlags, (instr.SetsFlags ? "s" : "") + padding);
                        ImGui.SameLine(0f, 0f);

                        for (int i = 0; i < instr.Operands.Count; i++)
                        {
                            string operand = instr.Operands[i];

                            foreach (var token in OperandTokenizer.GetTokens(operand))
                            {
                                ImGui.TextColored(GetColorForToken(token.Type), token.Text);
                                ImGui.SameLine(0f, 0f);
                            }

                            if (i < instr.Operands.Count - 1)
                            {
                                ImGui.TextColored(new Vector4(1f), ", ");
                                ImGui.SameLine(0.0f, 0.0f);
                            }
                        }
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

        // random instructions for testing
        private readonly List<MockInstruction> _mockDisasm =
        [
            new(0x08000000, 0xE8B0001E, "ldm", "ge", ["r0!", "{ r1-r4 }"]),
            new(0x08000004, 0xE3A02004, "mov", "", ["r2", "#4"]),
            new(0x08000008, 0xEA000003, "b", "", ["0x08000018"]),
            new(0x0800000C, 0xE3520000, "cmp", "ne", ["r2", "#0"]),
            new(0x08000010, 0x1A000001, "b", "ne", ["0x08000018"]),
            new(0x08000014, 0xE2823001, "add", "", ["r3", "r2", "#1"], true),
            new(0x08000018, 0xE1A00003, "mov", "", ["r0", "r3"]),
            new(0x0800001C, 0xE12FFF1E, "bx", "", ["lr"]),
            new(0x08000020, 0xE59F0000, "ldr", "", ["r0", "[pc, #4]"]),
            new(0x08000024, 0xE3A01001, "mov", "eq", ["r1", "#1"]),
            new(0x08000028, 0xE3A030FF, "mov", "", ["r3", "#0xFF"]),
            new(0x0800002C, 0xE1A02003, "mov", "", ["r2", "r3"]),
            new(0x08000030, 0xE2444001, "sub", "", ["r4", "r4", "#1"]),
            new(0x08000034, 0xE3540000, "cmp", "", ["r4", "#0"]),
            new(0x08000038, 0x0AFFFFFA, "b", "eq", ["0x08000020"]),
            new(0x0800003C, 0xE3A0500A, "mov", "", ["r5", "#10"]),
            new(0x08000040, 0xE1A06005, "mov", "", ["r6", "r5"]),
            new(0x08000044, 0xE5867000, "str", "", ["r7", "[r6]"]),
            new(0x08000048, 0xE5968000, "ldr", "", ["r8", "[r6]"]),
            new(0x0800004C, 0xE1A09008, "mov", "", ["r9", "r8"]),
            new(0x08000050, 0xE3A0A00F, "mov", "", ["r10", "#15"]),
            new(0x08000054, 0xE35A000F, "cmp", "", ["r10", "#15"]),
            new(0x08000058, 0x1A000002, "b", "ne", ["0x08000068"]),
            new(0x0800005C, 0xE3A0B001, "mov", "", ["r11", "#1"]),
            new(0x08000060, 0xE58BB000, "str", "", ["r11", "[r11]"]),
            new(0x08000064, 0xEAFFFFF0, "b", "", ["0x08000028"]),
        ];
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

        [GeneratedRegex(@"(r\d+|lr|sp|pc|#(?:0x)?[0-9A-Fa-f]+|0x[0-9A-Fa-f]+|[\[\]\{\},!]|[-+]| )", RegexOptions.Compiled)]
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
            "!" or "-" or "+" or " "             => OperandTokenType.Symbol,
            _ when LabelRegex.IsMatch(token)     => OperandTokenType.Label,
            _                                    => OperandTokenType.Unknown
        };
    }

    internal struct MockInstruction(uint address, uint opcode, string mnemonicBase, string conditionCode, List<string> operands, bool setsFlags = false)
    {
        public uint Address = address;
        public uint Opcode = opcode;
        public string MnemonicBase = mnemonicBase;
        public string ConditionCode = conditionCode;
        public bool SetsFlags = setsFlags;
        public List<string> Operands = operands;
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