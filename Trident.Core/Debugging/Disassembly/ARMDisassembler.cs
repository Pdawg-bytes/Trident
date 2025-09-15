using Trident.Core.Global;
using Trident.CodeGeneration.Shared;

using static Trident.Core.CPU.Conditions;

using InstructionData = (string Mnemonic, System.Collections.Generic.List<string> Operands);
using Trident.Core.CPU;

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
                ARMGroup.BranchExchange      => BranchExchange(opcode),
                ARMGroup.BranchWithLink      => BranchWithLink(opcode, address),
                ARMGroup.Multiply            => Multiply(opcode),
                ARMGroup.MultiplyLong        => MultiplyLong(opcode),
                ARMGroup.SingleDataTrasnfer  => SingleDataTransfer(opcode),
                ARMGroup.SmallSignedTransfer => SignedDataTransfer(opcode),
                ARMGroup.BlockDataTransfer   => BlockDataTransfer(opcode),
                ARMGroup.Swap                => Swap(opcode),
                ARMGroup.SoftwareInterrupt   => SoftwareInterrupt(opcode),
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

        private static string ShiftedRegister(uint shiftData)
        {
            uint rm        = shiftData & 0x0F;
            bool regShift  = shiftData.IsBitSet(4);
            ShiftType type = (ShiftType)((shiftData >> 5) & 0b11);

            string mnemonic = new[] { "lsl", "lsr", "asr", "ror" }[(int)type];

            if (regShift)
            {
                string rs = _registers[(shiftData >> 8) & 0x0F];
                return $"{_registers[rm]}, {mnemonic} {rs}";
            }

            uint shamt = (shiftData >> 7) & 0x1F;

            if (shamt == 0 && (type == ShiftType.LSR || type == ShiftType.ASR))
                shamt = 32;

            if (shamt == 0)
            {
                return type == ShiftType.ROR
                    ? $"{_registers[rm]}, rrx"
                    : _registers[rm];
            }

            return $"{_registers[rm]}, {mnemonic} #{shamt}";
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


        #region Multiply
        private static InstructionData Multiply(uint opcode)
        {
            string rm       = _registers[opcode & 0x0F];
            string rs       = _registers[(opcode >> 8) & 0x0F];
            string rn       = _registers[(opcode >> 12) & 0x0F];
            string rd       = _registers[(opcode >> 16) & 0x0F];
            string setFlags = opcode.IsBitSet(20) ? "s": "";
            bool accumulate = opcode.IsBitSet(21);

            string mnemonic =
                (accumulate ? "mla" : "mul") +
                setFlags;

            return (mnemonic, [ rd, rm, rs, (accumulate) ? rn : "" ]);
        }

        private static InstructionData MultiplyLong(uint opcode)
        {
            string rm = _registers[opcode & 0x0F];
            string rs = _registers[(opcode >> 8) & 0x0F];
            string rdLo = _registers[(opcode >> 12) & 0x0F];
            string rdHi = _registers[(opcode >> 16) & 0x0F];
            string setFlags = opcode.IsBitSet(20) ? "s" : "";
            bool accumulate = opcode.IsBitSet(21);

            string mnemonic = (new[] { "umull", "umlal", "smull", "smlal" }[(opcode >> 21) & 0b11]) + setFlags;
            return (mnemonic, [ rdLo, rdHi, rm, rs ]);
        }
        #endregion


        #region Data transfer
        private static InstructionData SingleDataTransfer(uint opcode)
        {
            uint data        = opcode & 0x0FFF;
            uint rd          = (opcode >> 12) & 0x0F;
            uint rn          = (opcode >> 16) & 0x0F;
            bool load        = opcode.IsBitSet(20);
            string writeback = opcode.IsBitSet(21) ? "!" : "";
            bool byteMode    = opcode.IsBitSet(22);
            bool add         = opcode.IsBitSet(23);
            bool preIndex    = opcode.IsBitSet(24);
            bool immediate   = opcode.IsBitSet(25);

            string offset = immediate
                ? (add ? ShiftedRegister(data) : $"-{ShiftedRegister(data)}")
                : (add ? $"#0x{data:X}" : $"-#0x{data:X}");

            string mnemonic =
                (load ? "ldr" : "str") +
                (byteMode ? "b" : "");

            if (preIndex)
                return (mnemonic, [_registers[rd], $"[{_registers[rn]}, {offset}]{writeback}"]);
            else
                return (mnemonic, [_registers[rd], $"[{_registers[rn]}]{writeback}", offset]);
        }

        private static InstructionData SignedDataTransfer(uint opcode)
        {
            bool halfWord    = opcode.IsBitSet(5);
            bool signed      = opcode.IsBitSet(6);
            uint rd          = (opcode >> 12) & 0x0F;
            uint rn          = (opcode >> 16) & 0x0F;
            bool load        = opcode.IsBitSet(20);
            string writeback = opcode.IsBitSet(21) ? "!" : "";
            bool immediate   = opcode.IsBitSet(22);
            bool add         = opcode.IsBitSet(23);
            bool preIndex    = opcode.IsBitSet(24);

            string offset;
            if (immediate)
            {
                int imm = (int)(((opcode >> 4) & 0xF0) | (opcode & 0x0F));
                offset = $"#{(add ? "" : "-")}0x{imm:X2}";
            }
            else
                offset = $"{(add ? "" : "-")}{_registers[opcode & 0x0F]}";

            string mnemonic =
                (load     ? "ldr" : "str") +
                (signed   ? 's'   : "") +
                (halfWord ? 'h'   : 'b');

            if (preIndex)
                return (mnemonic, [_registers[rd], $"[{_registers[rn]}, {offset}]{writeback}"]);
            else
                return (mnemonic, [_registers[rd], $"[{_registers[rn]}]", offset]);
        }

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