using Trident.Core.Global;
using Trident.CodeGeneration.Shared;
using System.Runtime.CompilerServices;

using static Trident.Core.Debugging.Disassembly.DisassemblerUtilities;

using InstructionData = (string Mnemonic, System.Collections.Generic.List<string> Operands);

namespace Trident.Core.Debugging.Disassembly
{
    internal static class ThumbDisassembler
    {
        private static readonly string[] _shiftMnemonics           = ["lsl", "lsl", "asr", "???"];
        private static readonly string[] _immediateOpMnemonics     = ["mov", "cmp", "add", "sub"];
        private static readonly string[] _aluMnemonics             = ["and", "eor", "lsl", "lsr", "asr", "adc", "sbc", "ror", "tst", "neg", "cmp", "cmn", "orr", "mul", "bic", "mvn"];
        private static readonly string[] _highRegisterMnemonics    = ["add", "cmp", "mov", "bx"];
        private static readonly string[] _loadStoreRegMnemonics    = ["str", "strb", "ldr", "ldrb"];
        private static readonly string[] _loadStoreSignedMnemonics = ["strh", "ldrsb", "ldrh", "ldrsh"];
        private static readonly string[] _loadStoreImmOffMnemonics = ["str", "ldr", "strb", "ldrb"];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DisassembledInstruction Disassemble(uint address, uint lr, ushort opcode) =>
            Disassemble(address, lr, opcode, ThumbDecoder.DetermineThumbGroup(opcode));

        internal static DisassembledInstruction Disassemble(uint address, uint lr, ushort opcode, ThumbGroup group)
        {
            /*var instr = new DisassembledInstruction
            {
                Address = address,
                Opcode = opcode,
                MnemonicBase = "??",
                ConditionCode = "",
                Operands = ["??"]
            };

            InstructionData data = group switch
            {
                ThumbGroup.ShiftImmediate      => ShiftImmediate(opcode),
                ThumbGroup.AddSubtract         => AddSubtract(opcode),
                ThumbGroup.ImmediateOperations => ImmediateOperations(opcode),
                ThumbGroup.ThumbALU            => DataProcessing(opcode),
                ThumbGroup.HiRegisterOps       => HighRegister(opcode),
                ThumbGroup.LoadPCRelative      => LoadPCRelative(opcode, address),
                ThumbGroup.LoadStoreRegOffset  => LoadStoreRegOrSigned(opcode, false),
                ThumbGroup.LoadStoreSigned     => LoadStoreRegOrSigned(opcode, true),
                ThumbGroup.LoadStoreImmOffset  => LoadStoreImmOffset(opcode),
                ThumbGroup.LoadStore16         => LoadStore16(opcode),
                ThumbGroup.LoadStoreSPRelative => LoadStoreSPRelative(opcode),
                ThumbGroup.LoadAddress         => LoadAddress(opcode, address),
                ThumbGroup.AddOffsetSP         => AddOffsetSP(opcode),
                ThumbGroup.PushPop             => PushPop(opcode),
                ThumbGroup.LoadStoreMultiple   => LoadStoreMultiple(opcode),
                ThumbGroup.ConditionalBranch   => ConditionalBranch(opcode, address, ref instr),
                ThumbGroup.SoftwareInterrupt   => SoftwareInterrupt(opcode),
                ThumbGroup.UnconditionalBranch => UnconditionalBranch(opcode, address),
                ThumbGroup.LongBranchWithLink  => LongBranchWithLink(opcode, lr),
                ThumbGroup.BranchExchange      => BranchExchange(opcode),
                _ => new InstructionData { Opcode = "??", Operands = ["??"] }
            };

            instr.MnemonicBase = data.Opcode;
            instr.Operands = data.Operands;
            return instr;*/

            return new DisassembledInstruction
            {
                Address = address,
                Opcode = opcode,
            };
        }


        #region Shift
        private static InstructionData ShiftImmediate(uint opcode)
        {
            uint rd    = opcode & 0b111;
            uint rs    = (opcode >> 3) & 0b111;
            uint shamt = (opcode >> 6) & 0x1F;

            return (_shiftMnemonics[(opcode >> 11) & 0b11], [_registers[rd], _registers[rs], $"#{shamt}"]);
        }
        #endregion


        #region Arithmetic
        private static InstructionData AddSubtract(uint opcode)
        {
            uint rd        = opcode & 0b111;
            uint rs        = (opcode >> 3) & 0b111;
            uint data      = (opcode >> 6) & 0x1F;
            bool subtract  = opcode.IsBitSet(9);
            bool immediate = opcode.IsBitSet(10);

            return (subtract ? "sub" : "add", [_registers[rd], _registers[rs], immediate ? $"#0x{data}" : _registers[data]]);
        }

        private static InstructionData LoadAddress(uint opcode, uint address)
        {
            uint offset = (opcode & 0xFF) << 2;
            uint rd = (opcode >> 8) & 0b111;
            bool useSP = opcode.IsBitSet(11);

            return ("add", [_registers[rd], useSP ? "sp" : "pc", $"#0x{offset:X}"]);
        }

        private static InstructionData AddOffsetSP(uint opcode)
        {
            uint offset = (opcode & 0x3F) << 2;
            string sign = opcode.IsBitSet(7) ? "-" : "";

            return ("add", ["sp", $"#{sign}0x{offset:X}"]);
        }
        #endregion


        #region Data processing
        private static InstructionData DataProcessing(uint opcode)
        {
            uint rd = opcode & 0b111;
            uint rs = (opcode >> 3) & 0b111;
            uint op = (opcode >> 6) & 0x0F;

            return (_aluMnemonics[op], [_registers[rd], _registers[rs]]);
        }
        #endregion


        #region Load/Store
        private static InstructionData LoadPCRelative(uint opcode, uint address)
        {
            byte offset = (byte)opcode;
            uint rd     = (opcode >> 8) & 0b111;

            return ("ldr", [_registers[rd], $"[pc, #0x{offset:X}]"]);
        }

        private static InstructionData LoadStoreRegOrSigned(uint opcode, bool signed)
        {
            uint rd = opcode & 0b111;
            uint rb = (opcode >> 3) & 0b111;
            uint ro = (opcode >> 6) & 0b111;
            uint op = (opcode >> 10) & 0b11;

            return 
            (
                signed 
                    ? _loadStoreSignedMnemonics[op] 
                    : _loadStoreRegMnemonics[op], 

                [_registers[rd], $"[{_registers[rb]}, {_registers[ro]}]"]
            );
        }

        private static InstructionData LoadStoreImmOffset(uint opcode)
        {
            uint rd     = opcode & 0b111;
            uint rb     = (opcode >> 3) & 0b111;
            uint offset = (opcode >> 6) & 0x1F;
            uint op     = (opcode >> 11) & 0b11;

            offset <<= (int)(~opcode & 0b11u);

            return (_loadStoreImmOffMnemonics[op], [_registers[rd], $"[{_registers[rb]}, #0x{offset:X}]"]);
        }

        private static InstructionData LoadStore16(uint opcode)
        {
            uint rd     = opcode & 0b111;
            uint rb     = (opcode >> 3) & 0b111;
            uint offset = ((opcode >> 6) & 0x1F) << 1;
            bool load   = opcode.IsBitSet(11);

            return (load ? "ldrh" : "strh", [_registers[rd], $"[{_registers[rb]}, #0x{offset:X}]"]);
        }

        private static InstructionData LoadStoreSPRelative(uint opcode)
        {
            uint offset = (opcode & 0xFF) << 2;
            uint rd     = (opcode >> 8) & 0b111;
            bool load   = opcode.IsBitSet(11);

            return (load ? "ldr" : "str", [_registers[rd], $"[sp, #0x{offset:X}]"]);
        }
        #endregion


        #region Miscellaneous
        private static InstructionData ImmediateOperations(uint opcode)
        {
            byte immediate = (byte)opcode;
            uint rd = (opcode >> 8) & 0b111;

            return (_immediateOpMnemonics[(opcode >> 11) & 0b11], [_registers[rd], $"#0x{immediate:X}"]);
        }

        private static InstructionData HighRegister(uint opcode)
        {
            uint rd = (opcode & 0b111) | (opcode.IsBitSet(7) ? 8 : 0u);
            uint rs = ((opcode >> 3) & 0b111) | (opcode.IsBitSet(6) ? 8 : 0u);
            uint op = (opcode >> 8) & 0b11;

            return (_highRegisterMnemonics[op], [_registers[rd], _registers[rs]]);
        }
        #endregion


        #region Block transfer
        private static InstructionData PushPop(uint opcode)
        {
            uint regList = (byte)opcode;
            uint rBit = (opcode >> 8) & 1;
            bool pop = opcode.IsBitSet(11);

            regList |= rBit << (pop ? 15 : 14);

            return (pop ? "pop" : "push", [RegisterList(regList)]);
        }

        private static InstructionData LoadStoreMultiple(uint opcode)
        {
            uint regList = (byte)opcode;
            uint rb      = (opcode >> 8) & 0b111;
            bool load    = opcode.IsBitSet(11);

            return (load ? "ldmia" : "stmia", [$"{_registers[rb]}!", RegisterList(regList)]);
        }
        #endregion


        #region Software interrupt
        private static InstructionData SoftwareInterrupt(uint opcode) => ("swi", [$"#0x{(byte)opcode:X}"]);
        #endregion


        #region Branch
        private static InstructionData ConditionalBranch(uint opcode, uint address, ref DisassembledInstruction instr)
        {
            //instr.ConditionCode = ConditionCodeString((opcode >> 8) & 0x0F);
            uint offset = (uint)(opcode & 0xFF).ExtendFrom(8) << 1;

            return ("b", [$"0x{((address + offset) + 4):X8}"]);
        }

        private static InstructionData UnconditionalBranch(uint opcode, uint address)
        {
            uint offset = (uint)(opcode & 0x07FF).ExtendFrom(11) << 1;
            return ("b", [$"0x{((address + offset) + 4):X8}"]);
        }

        private static InstructionData LongBranchWithLink(uint opcode, uint lr)
        {
            uint offset          = (opcode & 0x07FF) << 1;
            bool completesBranch = opcode.IsBitSet(11);

            return (completesBranch ? "blh" : "bll", [completesBranch ? $"0x{lr + offset:X}" : ""]);
        }

        private static InstructionData BranchExchange(uint opcode) => ("bx", [_registers[((opcode >> 3) & 0b111) | (opcode.IsBitSet(6) ? 8 : 0u)]]);
        #endregion
    }
}