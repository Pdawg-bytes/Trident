using Trident.Core.Global;
using Trident.CodeGeneration.Shared;
using System.Runtime.CompilerServices;
using Trident.Core.Debugging.Disassembly.Tokens;

using static Trident.Core.Debugging.Disassembly.DisassemblerUtilities;

namespace Trident.Core.Debugging.Disassembly
{
    internal static class ARMDisassembler
    {
        private static readonly string[] _dataProcessingMnemonics = ["and", "eor", "sub", "rsb", "add", "adc", "sbc", "rsc", "tst", "teq", "cmp", "cmn", "orr", "mov", "bic", "mvn"];
        private static readonly string[] _multiplyLongMnemonics   = ["umull", "umlal", "smull", "smlal"];
        private static readonly string[] _blockTransferModes      = ["da", "ia", "db", "ib"];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DisassembledInstruction Disassemble(uint address, uint opcode, byte[] tokenBuffer) =>
            Disassemble(address, opcode, ARMDecoder.DetermineARMGroup(opcode), tokenBuffer);

        internal static DisassembledInstruction Disassemble(uint address, uint opcode, ARMGroup group, byte[] tokenBuffer)
        {
            WriteResult result = group switch
            {
                ARMGroup.BranchExchange      => BranchExchange(opcode, tokenBuffer),
                ARMGroup.BranchWithLink      => BranchWithLink(opcode, address, tokenBuffer),
                //ARMGroup.DataProcessing      => DataProcessing(opcode, address, tokenBuffer),
                ARMGroup.PSRTransfer         => PSRTransfer(opcode, tokenBuffer),
                ARMGroup.Multiply            => Multiply(opcode, tokenBuffer),
                ARMGroup.MultiplyLong        => MultiplyLong(opcode, tokenBuffer),
                ARMGroup.SingleDataTrasnfer  => SingleDataTransfer(opcode, tokenBuffer),
                ARMGroup.SmallSignedTransfer => SignedDataTransfer(opcode, tokenBuffer),
                ARMGroup.BlockDataTransfer   => BlockDataTransfer(opcode, tokenBuffer),
                ARMGroup.Swap                => Swap(opcode, tokenBuffer),
                ARMGroup.SoftwareInterrupt   => SoftwareInterrupt(opcode, tokenBuffer),
                _ => default
            };


            ReadOnlySpan<char> condText = ConditionCodeString(opcode >> 28);
            if (!condText.IsEmpty)
            {
                int condLen = 2 + condText.Length;

                if (result.BytesWritten + condLen > tokenBuffer.Length)
                    throw new InvalidOperationException($"Not enough space in token buffer to insert condition token (need {condLen} bytes).");

                for (int i = result.BytesWritten - 1; i >= result.OperandsStartIndex; i--)
                    tokenBuffer[i + condLen] = tokenBuffer[i];

                var slice = tokenBuffer.AsSpan(result.OperandsStartIndex);
                var dummyWriter = new TokenWriter(slice);
                dummyWriter.AppendFormatted(new Mnemonic(condText, isCondition: true));

                result.BytesWritten += condLen;
                result.OperandsStartIndex += condLen;
            }

            return new DisassembledInstruction
            {
                Address = address,
                Opcode = opcode,

                Tokens = new ReadOnlyMemory<byte>(tokenBuffer, 0, result.BytesWritten),
                OperandsStartIndex = result.OperandsStartIndex
            };
        }

        
        #region Branch
        private static WriteResult BranchExchange(uint opcode, Span<byte> buffer) =>
            WriterHost.Write(buffer, $"{new Mnemonic("bx")} | {new Register(opcode & 0x0F)}");

        private static WriteResult BranchWithLink(uint opcode, uint address, Span<byte> buffer)
        {
            int offset = ((opcode & 0xFFFFFF).ExtendFrom(24)) << 2;
            bool link  = opcode.IsBitSet(24);

            Mnemonic mnemonic = new(link ? "bl" : "b");
            Number target     = new((address + (uint)offset) + 8, isLabel: true);

            return WriterHost.Write(buffer, $"{mnemonic} | {target}");
        }
        #endregion


        #region Data processing
        /*private static WriteResult DataProcessing(uint opcode, uint address, Span<byte> buffer)
        {
            uint rd         = (opcode >> 12) & 0x0F;
            uint rn         = (opcode >> 16) & 0x0F;
            string setFlags = opcode.IsBitSet(20) ? "s" : "";
            bool immediate  = opcode.IsBitSet(25);
            ALUOpARM op     = (ALUOpARM)((opcode >> 21) & 0x0F);

            string mnemonic = _dataProcessingMnemonics[(int)op] + (((int)op >= 0b1000 && (int)op <= 0b1011) ? "" : setFlags);

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
        }*/
        #endregion


        #region PSR
        private static WriteResult PSRTransfer(uint opcode, Span<byte> buffer)
        {
            bool toPSR     = opcode.IsBitSet(21);
            bool useSpsr   = opcode.IsBitSet(22);
            bool immediate = opcode.IsBitSet(25);

            PSR psr = new(!useSpsr, toPSR ? (PSRFlags)((opcode >> 16) & 0x0F) : PSRFlags.None);

            if (!toPSR)
            {
                Register rd = new((opcode >> 12) & 0x0F);
                return WriterHost.Write(buffer, $"{new Mnemonic("mrs")} | {rd}, {psr}");
            }
            else
            {
                Mnemonic mnemonic = new("msr");

                return immediate ?
                    WriterHost.Write(buffer, $"{mnemonic} | {psr}, {new Number(RotatedImmediate(opcode))}") :
                    WriterHost.Write(buffer, $"{mnemonic} | {psr}, {new Register(opcode & 0x0F)}");
            }
        }
        #endregion


        #region Multiply
        private static WriteResult Multiply(uint opcode, Span<byte> buffer)
        {
            Mnemonic mnemonic = new(opcode.IsBitSet(21) ? "mla" : "mul");
            Mnemonic flags    = new(opcode.IsBitSet(20) ? "s" : "");

            Register rm = new(opcode & 0x0F);
            Register rs = new((opcode >> 8) & 0x0F);
            Register rn = new((opcode >> 12) & 0x0F);
            Register rd = new((opcode >> 16) & 0x0F);

            if (opcode.IsBitSet(21))
                return WriterHost.Write(buffer, $"{mnemonic}{flags} | {rd}, {rm}, {rs}, {rn}");
            else
                return WriterHost.Write(buffer, $"{mnemonic}{flags} | {rd}, {rm}, {rs}");
        }

        private static WriteResult MultiplyLong(uint opcode, Span<byte> buffer)
        {
            Mnemonic mnemonic = new(_multiplyLongMnemonics[(opcode >> 21) & 0b11]);
            Mnemonic flags    = new(opcode.IsBitSet(20) ? "s" : "");

            Register rm   = new(opcode & 0x0F);
            Register rs   = new((opcode >> 8) & 0x0F);
            Register rdLo = new((opcode >> 12) & 0x0F);
            Register rdHi = new((opcode >> 16) & 0x0F);
            
            return WriterHost.Write(buffer, $"{mnemonic}{flags} | {rdLo}, {rdHi}, {rm}, {rs}");
        }
        #endregion


        #region Data transfer
        private static WriteResult SingleDataTransfer(uint opcode, Span<byte> buffer)
        {
            uint data      = opcode & 0x0FFF;
            uint rd        = (opcode >> 12) & 0x0F;
            uint rn        = (opcode >> 16) & 0x0F;
            bool load      = opcode.IsBitSet(20);
            bool writeback = opcode.IsBitSet(21);
            bool byteMode  = opcode.IsBitSet(22);
            bool add       = opcode.IsBitSet(23);
            bool preIndex  = opcode.IsBitSet(24);
            bool immediate = opcode.IsBitSet(25);

            TokenWriter writer = new(buffer);

            writer.AppendFormatted(new Mnemonic(load ? "ldr" : "str"));
            if (byteMode) writer.AppendFormatted(new Mnemonic("b"));

            writer.BeginOperands();
            writer.AppendFormatted(new Register(rd));
            writer.SyntaxSpace(',');

            writer.Syntax('[');
            writer.AppendFormatted(new Register(rn));


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AppendOffset(ref TokenWriter writer)
            {
                switch (immediate, add)
                {
                    case (true, true):   writer.AppendFormatted(new ShiftedOperand(data)); break;
                    case (true, false):  writer.Syntax('-'); writer.AppendFormatted(new ShiftedOperand(data)); break;
                    case (false, true):  if (data != 0) { writer.AppendFormatted(new Number(data)); } break;
                    case (false, false): if (data != 0) { writer.Syntax('-'); writer.AppendFormatted(new Number(data)); } break;
                }
            }

            if (preIndex)
            {
                writer.SyntaxSpace(',');
                AppendOffset(ref writer);
                writer.Syntax(']');

                if (writeback)
                    writer.Syntax('!');
            }
            else
            {
                writer.Syntax(']');

                if (writeback)
                    writer.SyntaxSpace('!');

                writer.SyntaxSpace(',');
                AppendOffset(ref writer);
            }

            return writer.Finalize();
        }

        private static WriteResult SignedDataTransfer(uint opcode, Span<byte> buffer)
        {
            bool halfWord  = opcode.IsBitSet(5);
            bool signed    = opcode.IsBitSet(6);
            uint rd        = (opcode >> 12) & 0x0F;
            uint rn        = (opcode >> 16) & 0x0F;
            bool load      = opcode.IsBitSet(20);
            bool writeback = opcode.IsBitSet(21);
            bool immediate = opcode.IsBitSet(22);
            bool add       = opcode.IsBitSet(23);
            bool preIndex  = opcode.IsBitSet(24);

            TokenWriter writer = new(buffer);

            writer.AppendFormatted(new Mnemonic(load ? "ldr" : "str"));
            if (signed)   writer.AppendFormatted(new Mnemonic("s"));
            if (halfWord) writer.AppendFormatted(new Mnemonic("h"));
            else          writer.AppendFormatted(new Mnemonic("b"));

            writer.BeginOperands();
            writer.AppendFormatted(new Register(rd));
            writer.SyntaxSpace(',');

            writer.Syntax('[');
            writer.AppendFormatted(new Register(rn));


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AppendOffset(ref TokenWriter writer)
            {
                if (immediate)
                {
                    int imm = (int)(((opcode >> 4) & 0xF0) | (opcode & 0x0F));
                    if (!add) writer.Syntax('-');
                    writer.AppendFormatted(new Number((uint)imm));
                }
                else
                {
                    uint rm = opcode & 0x0F;
                    if (!add) writer.Syntax('-');
                    writer.AppendFormatted(new Register(rm));
                }
            }

            if (preIndex)
            {
                writer.SyntaxSpace(',');
                AppendOffset(ref writer);
                writer.Syntax(']');

                if (writeback)
                    writer.Syntax('!');
            }
            else
            {
                writer.Syntax(']');

                if (writeback)
                    writer.SyntaxSpace('!');

                writer.SyntaxSpace(',');
                AppendOffset(ref writer);
            }

            return writer.Finalize();
        }

        private static WriteResult BlockDataTransfer(uint opcode, Span<byte> buffer)
        {
            uint regList   = (ushort)opcode;
            uint rn        = (opcode >> 16) & 0x0F;
            bool writeback = opcode.IsBitSet(21);
            bool userMode  = opcode.IsBitSet(22);
            uint addrMode  = (opcode >> 23) & 0b11;

            TokenWriter writer = new(buffer);

            writer.AppendFormatted(new Mnemonic(opcode.IsBitSet(20) ? "ldm" : "stm"));
            writer.AppendFormatted(new Mnemonic(_blockTransferModes[addrMode]));

            writer.BeginOperands();
            writer.AppendFormatted(new Register(rn));
            if (writeback)
                writer.Syntax('!');

            writer.SyntaxSpace(',');

            AppendRegisterList(ref writer, regList, userMode);

            return writer.Finalize();
        }
        #endregion
        

        #region Data swap
        private static WriteResult Swap(uint opcode, Span<byte> buffer)
        {
            Mnemonic mnemonic = new(opcode.IsBitSet(22) ? "swpb" : "swp");

            Register rm = new(opcode & 0x0F);
            Register rd = new((opcode >> 12) & 0x0F);
            Register rn = new((opcode >> 16) & 0x0F);

            return WriterHost.Write(buffer, $"{mnemonic} | {rd}, {rm}, [{rn}]");
        }
        #endregion


        #region Software interrupt
        private static WriteResult SoftwareInterrupt(uint opcode, Span<byte> buffer) =>
            WriterHost.Write(buffer, $"{new Mnemonic("swi")} | {new Number(opcode & 0x00FFFFFF)}");
        #endregion
    }
}