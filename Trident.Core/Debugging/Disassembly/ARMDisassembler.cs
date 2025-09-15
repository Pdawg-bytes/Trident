using Trident.Core.Global;
using Trident.CodeGeneration.Shared;

using static Trident.Core.CPU.Conditions;

using InstructionData = (string Mnemonic, System.Collections.Generic.List<string> Operands);

namespace Trident.Core.Debugging.Disassembly
{
    internal static class ARMDisassembler
    {
        private readonly static string[] _registers = [ "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12", "sp", "lr", "pc" ];

        internal static DisassembledInstruction Disassemble(uint address, uint opcode)
        {
            var instr = new DisassembledInstruction
            {
                Address = address,
                Opcode = opcode,
                MnemonicBase = "??",
                ConditionCode = ConditionCodeString(opcode >> 28),
                Operands = ["??"]
            };

            InstructionData data = ARMDecoder.DetermineARMGroup(opcode) switch
            {
                ARMGroup.BranchExchange    => BranchExchange(opcode),
                ARMGroup.BranchWithLink    => BranchWithLink(opcode, address),
                ARMGroup.BlockDataTransfer => BlockDataTransfer(opcode),
                ARMGroup.Swap              => Swap(opcode),
                ARMGroup.SoftwareInterrupt => SoftwareInterrupt(opcode),
                _ => new InstructionData { Mnemonic = "??", Operands = ["??"] }
            };

            instr.MnemonicBase = data.Mnemonic;
            instr.Operands = data.Operands;
            return instr;
        }

        private static string ConditionCodeString(uint condition) => condition switch
        {
            CondEQ => "eq",
            CondNE => "ne",
            CondCS => "cs",
            CondCC => "cc",
            CondMI => "mi",
            CondPL => "pl",
            CondVS => "vs",
            CondVC => "vc",
            CondHI => "hi",
            CondLS => "ls",
            CondGE => "ge",
            CondLT => "lt",
            CondGT => "gt",
            CondLE => "le",
            CondAL => "",
            _      => "??"
        };

        private static string RegisterList(uint rlist)
        {
            List<string> parts = [];
            int? rangeStart = null;
            int rangeLength = 0;

            for (int i = 0; i <= 16; i++) 
            {
                bool set = i < 16 && (rlist & (1u << i)) != 0;

                if (set)
                {
                    if (!rangeStart.HasValue)
                    {
                        rangeStart = i;
                        rangeLength = 1;
                    }
                    else
                        rangeLength++;
                }
                else if (rangeStart.HasValue)
                {
                    int start = rangeStart.Value;
                    int end = i - 1;

                    if (rangeLength >= 3)
                        parts.Add($"{_registers[start]}-{_registers[end]}");
                    else
                        for (int j = start; j <= end; j++)
                            parts.Add(_registers[j]);

                    rangeStart = null;
                    rangeLength = 0;
                }
            }

            return "{ " + string.Join(", ", parts) + " }";
        }


        #region Branch
        private static InstructionData BranchExchange(uint opcode) => ("bx", [ _registers[opcode & 0x0F] ]);

        private static InstructionData BranchWithLink(uint opcode, uint address)
        {
            int offset = ((opcode & 0xFFFFFF).ExtendFrom(24)) << 2;
            bool link  = opcode.IsBitSet(24);

            return (link ? "bl" : "b", [ $"0x{((address + (uint)offset) + 8):X8}" ]);
        }
        #endregion


        #region Data transfer
        private static InstructionData BlockDataTransfer(uint opcode)
        {
            uint regList   = (ushort)opcode;
            uint rn        = (opcode >> 16) & 0x0F;
            bool writeback = opcode.IsBitSet(21);
            bool userMode  = opcode.IsBitSet(22);
            uint addrMode  = (opcode >> 23) & 0b11;

            string mnemonic = opcode.IsBitSet(20) ? "ldm" : "stm";
            string suffix   = new[] { "da", "ia", "db", "ib" }[addrMode];

            List<string> operands = [];

            operands.Add(_registers[rn] + (writeback ? '!' : ""));
            operands.Add(RegisterList(regList) + (userMode ? '^' : ""));

            return (mnemonic + suffix, operands);
        }
        #endregion


        #region Data swap
        private static InstructionData Swap(uint opcode)
        {
            uint rm       = opcode & 0x0F;
            uint rd       = (opcode >> 12) & 0x0F;
            uint rn       = (opcode >> 16) & 0x0F;
            bool byteMode = opcode.IsBitSet(22);

            return (byteMode ? "swpb" : "swp", [ _registers[rd], _registers[rm], $"[{_registers[rn]}]" ]);
        }
        #endregion


        #region Software interrupt
        private static InstructionData SoftwareInterrupt(uint opcode) => ("swi", [ $"#0x{opcode & 0x00FFFFFF:X6}"] );
        #endregion
    }
}
