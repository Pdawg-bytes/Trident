using Trident.Core.Global;
using Trident.Core.CPU.Decoding;
using Trident.CodeGeneration.Shared;

using static Trident.Core.Debugging.Disassembly.DisassemblerUtilities;

using InstructionData = (string Mnemonic, System.Collections.Generic.List<string> Operands);

namespace Trident.Core.Debugging.Disassembly
{
    internal static class ARMDisassembler
    {
        private static readonly string[] _dataProcessingMnemonics = ["and", "eor", "sub", "rsb", "add", "adc", "sbc", "rsc", "tst", "teq", "cmp", "cmn", "orr", "mov", "bic", "mvn"];
        private static readonly string[] _multiplyLongMnemonics   = ["umull", "umlal", "smull", "smlal"];
        private static readonly string[] _blockTransferModes      = ["da", "ia", "db", "ib"];

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
                ARMGroup.DataProcessing      => DataProcessing(opcode, address),
                ARMGroup.PSRTransfer         => PSRTransfer(opcode),
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

        
        #region Branch
        private static InstructionData BranchExchange(uint opcode) => ("bx", [_registers[opcode & 0x0F]]);

        private static InstructionData BranchWithLink(uint opcode, uint address)
        {
            int offset = ((opcode & 0xFFFFFF).ExtendFrom(24)) << 2;
            bool link  = opcode.IsBitSet(24);

            return (link ? "bl" : "b", [$"0x{((address + (uint)offset) + 8):X8}"]);
        }
        #endregion


        #region Data processing
        private static InstructionData DataProcessing(uint opcode, uint address)
        {
            uint rd         = (opcode >> 12) & 0x0F;
            uint rn         = (opcode >> 16) & 0x0F;
            string setFlags = opcode.IsBitSet(20) ? "s" : "";
            bool immediate  = opcode.IsBitSet(25);
            ALUOpARM op     = (ALUOpARM)((opcode >> 21) & 0x0F);

            string mnemonic = _dataProcessingMnemonics[(int)op] + setFlags;

            string operand;
            if (immediate)
            {
                uint value = RotatedImmediate(opcode);
                if (rn == 15)
                {
                    if (op == ALUOpARM.SUB) value = address - value;
                    if (op == ALUOpARM.ADD) value = address + value;
                }
                operand = $"#0x{value:X}";
            }
            else
                operand = ShiftedRegister(opcode & 0x0FFF);

            return op switch
            {
                ALUOpARM.ADD or ALUOpARM.SUB when rn == 15 && immediate
                    => (mnemonic, [_registers[rd], operand]),

                ALUOpARM.TST or ALUOpARM.TEQ or ALUOpARM.CMP or ALUOpARM.CMN
                    => (mnemonic, [_registers[rn], operand]),

                ALUOpARM.MOV or ALUOpARM.MVN
                    => (mnemonic, [_registers[rd], operand]),

                _ => (mnemonic, [_registers[rd], _registers[rn], operand])
            };
        }
        #endregion


        #region PSR
        private static InstructionData PSRTransfer(uint opcode)
        {
            bool toPSR = opcode.IsBitSet(21);
            bool useSpsr = opcode.IsBitSet(22);
            bool immediate = opcode.IsBitSet(25);

            string psrName = useSpsr ? "spsr" : "cpsr";

            if (!toPSR)
            {
                string rd = _registers[(opcode >> 12) & 0x0F];
                return ("mrs", [rd, psrName]);
            }
            else
            {
                string operand = immediate
                    ? $"#0x{RotatedImmediate(opcode):X}"
                    : _registers[opcode & 0x0F];

                return ("msr", [psrName + BuildFSXC((opcode >> 16) & 0x0F), operand]);
            }
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

            return (mnemonic, [rd, rm, rs, (accumulate ? rn : "")]);
        }

        private static InstructionData MultiplyLong(uint opcode)
        {
            string rm       = _registers[opcode & 0x0F];
            string rs       = _registers[(opcode >> 8) & 0x0F];
            string rdLo     = _registers[(opcode >> 12) & 0x0F];
            string rdHi     = _registers[(opcode >> 16) & 0x0F];
            string setFlags = opcode.IsBitSet(20) ? "s" : "";
            bool accumulate = opcode.IsBitSet(21);

            string mnemonic = _multiplyLongMnemonics[(opcode >> 21) & 0b11] + setFlags;
            return (mnemonic, [rdLo, rdHi, rm, rs]);
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
                offset = $"#{(add ? "" : "-")}0x{imm:X}";
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
            string suffix   = _blockTransferModes[addrMode];

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

            return (byteMode ? "swpb" : "swp", [_registers[rd], _registers[rm], $"[{_registers[rn]}]"]);
        }
        #endregion


        #region Software interrupt
        private static InstructionData SoftwareInterrupt(uint opcode) => ("swi", [$"#0x{opcode & 0x00FFFFFF:X}"]);
        #endregion
    }
}