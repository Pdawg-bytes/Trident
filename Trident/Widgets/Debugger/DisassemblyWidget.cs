using ImGuiNET;
using System.Numerics;

namespace Trident.Widgets.Debugger
{
    internal class DisassemblyWidget : IWidget
    {
        private readonly ImFontPtr _monoFont;

        private readonly uint _currentInstructionHighlight = ImGui.ColorConvertFloat4ToU32(new(0.20f, 0.18f, 0.24f, 1.0f));
        private readonly Vector4 _colorAddress = new(0.60f, 0.70f, 0.95f, 1f);
        private readonly Vector4 _colorOpcode = new(0.75f, 0.75f, 0.80f, 1f);
        private readonly Vector4 _colorMnemonic = new(0.95f, 0.70f, 0.50f, 1f);
        private readonly Vector4 _colorCondition = new(0.95f, 0.90f, 0.60f, 1f);
        private readonly Vector4 _colorFlags = new(0.70f, 0.85f, 1.00f, 1f);
        private readonly Vector4 _colorOperands = new(0.70f, 0.95f, 0.75f, 1f);

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
                if (ImGui.Button("Step Cycle"))
                {
                    // TODO: step
                }

                ImGui.Checkbox("Show Address", ref _showAddress);
                ImGui.SameLine(); 
                ImGui.Checkbox("Show Bytecode", ref _showOpcode);

                ImGui.Separator();

                ImGui.BeginChild("DisasmScroll");

                if (ImGui.BeginTable("DisasmTable", 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
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
                        string flags = instr.Flags;

                        const int targetColumn = 8;
                        string padding = new string(' ', Math.Max(0, targetColumn - (mnemonic.Length + cond.Length + flags.Length)));

                        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

                        ImGui.PushStyleColor(ImGuiCol.Text, _colorMnemonic);
                        ImGui.Text(mnemonic);
                        ImGui.PopStyleColor();
                        ImGui.SameLine();

                        ImGui.PushStyleColor(ImGuiCol.Text, _colorCondition);
                        ImGui.Text(cond);
                        ImGui.PopStyleColor();
                        ImGui.SameLine();

                        ImGui.PushStyleColor(ImGuiCol.Text, _colorFlags);
                        ImGui.Text(flags + padding);
                        ImGui.PopStyleColor();
                        ImGui.SameLine();

                        ImGui.PushStyleColor(ImGuiCol.Text, _colorOperands);
                        ImGui.Text(instr.Operands);
                        ImGui.PopStyleColor();

                        ImGui.PopStyleVar();
                    }

                    ImGui.EndTable();
                    ImGui.PopFont();
                }

                ImGui.EndChild();
            }

            ImGui.End();
        }


        // random instructions for testing
        private readonly List<MockInstruction> _mockDisasm =
        [
            new(0x08000000, 0xE8B0001E, "ldm", "ge", "", "r0!, { r1-r4 }"),
            new(0x08000004, 0xEAFFFFFE, "b", "ne", "", "0x08000008"),
            new(0x08000008, 0xE3A00001, "mov", "", "", "r0, #1"),
            new(0x0800000C, 0xE2801004, "add", "", "s", "r1, r0, #4"),
            new(0x08000010, 0xE5912000, "ldr", "pl", "", "r2, [r1]"),
            new(0x08000014, 0xE3520000, "cmp", "eq", "", "r2, #0"),
            new(0x08000018, 0x1A000003, "b", "lt", "", "0x08000028"),
            new(0x0800001C, 0xE3A03005, "mov", "", "", "r3, #5"),
            new(0x08000020, 0xE5833000, "str", "", "", "r3, [r3]"),
            new(0x08000024, 0xEAFFFFF5, "b", "ne", "", "0x08000000"),
            new(0x08000028, 0xE1A04002, "mov", "", "", "r4, r2"),
            new(0x0800002C, 0xE2444001, "sub", "mi", "s", "r4, r4, #1"),
            new(0x08000030, 0xE3540000, "cmp", "hi", "", "r4, #0"),
            new(0x08000034, 0x0A000002, "beq", "", "", "0x08000044"),
            new(0x08000038, 0xE3A0500A, "mov", "", "", "r5, #10"),
            new(0x0800003C, 0xE2855001, "add", "", "", "r5, r5, #1"),
            new(0x08000040, 0xEAFFFFF0, "b", "vc", "", "0x08000008"),
            new(0x08000044, 0xE1A06005, "mov", "", "", "r6, r5"),
            new(0x08000048, 0xE5967000, "ldr", "ls", "", "r7, [r6]"),
            new(0x0800004C, 0xE1A00007, "mov", "", "", "r0, r7"),
            new(0x08000050, 0xE12FFF1E, "bx", "", "", "lr"),
            new(0x08000054, 0xE3A0C0FF, "mov", "", "", "r12, #0xFF"),
            new(0x08000058, 0xE58DC000, "str", "", "", "r12, [sp]"),
            new(0x0800005C, 0xE59F0004, "ldr", "eq", "", "r0, [pc, #4]"),
            new(0x08000060, 0xE1A01000, "mov", "", "", "r1, r0"),
            new(0x08000064, 0xE3A02001, "mov", "", "", "r2, #1"),
            new(0x08000068, 0xE1A03002, "mov", "", "", "r3, r2"),
            new(0x0800006C, 0xE1A00003, "mov", "", "", "r0, r3"),
            new(0x08000070, 0xE12FFF1E, "bx", "", "", "lr"),
        ];
    }


    internal struct MockInstruction(uint address, uint opcode, string mnemonicBase, string conditionCode, string flags, string operands)
    {
        public uint Address = address;
        public uint Opcode = opcode;
        public string MnemonicBase = mnemonicBase;
        public string ConditionCode = conditionCode;
        public string Flags = flags;
        public string Operands = operands;
    }
}