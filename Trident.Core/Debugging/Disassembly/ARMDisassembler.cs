using Trident.Core.Global;
using Trident.Core.CPU.Decoding;
using Trident.CodeGeneration.Shared;
using System.Runtime.CompilerServices;
using Trident.Core.Debugging.Disassembly.Tokens;

using static Trident.Core.Debugging.Disassembly.DisassemblerUtilities;

namespace Trident.Core.Debugging.Disassembly;

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
            ARMGroup.DataProcessing      => DataProcessing(opcode, address, tokenBuffer),
            ARMGroup.PSRTransfer         => PSRTransfer(opcode, tokenBuffer),
            ARMGroup.Multiply            => Multiply(opcode, tokenBuffer),
            ARMGroup.MultiplyLong        => MultiplyLong(opcode, tokenBuffer),
            ARMGroup.SingleDataTrasnfer  => SingleDataTransfer(opcode, tokenBuffer),
            ARMGroup.SmallSignedTransfer => SignedDataTransfer(opcode, tokenBuffer),
            ARMGroup.BlockDataTransfer   => BlockDataTransfer(opcode, tokenBuffer),
            ARMGroup.Swap                => Swap(opcode, tokenBuffer),
            ARMGroup.SoftwareInterrupt   => SoftwareInterrupt(opcode, tokenBuffer),

            _ => EmitUnknownInstruction(tokenBuffer)
        };

        InsertConditionMnemonic(opcode >> 28, tokenBuffer, ref result);

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
    private static WriteResult DataProcessing(uint opcode, uint address, Span<byte> buffer)
    {
        uint rd = (opcode >> 12) & 0x0F;
        uint rn = (opcode >> 16) & 0x0F;
        bool setFlags = opcode.IsBitSet(20);
        bool immediate = opcode.IsBitSet(25);
        ALUOpARM op = (ALUOpARM)((opcode >> 21) & 0x0F);

        TokenWriter writer = new(buffer);

        writer.AppendFormatted(new Mnemonic(_dataProcessingMnemonics[(int)op]));
        if (setFlags && (op < ALUOpARM.TST || op > ALUOpARM.CMN))
            writer.AppendFormatted(new Mnemonic("s"));

        writer.BeginOperands();
        if (immediate)
        {
            uint value = RotatedImmediate(opcode);
            if (rn == 15)
            {
                if (op == ALUOpARM.SUB) value = address - value;
                if (op == ALUOpARM.ADD) value = address + value;
            }

            switch (op)
            {
                case ALUOpARM.ADD or ALUOpARM.SUB when rn == 15:
                    writer.AppendFormatted(new Register(rd));
                    writer.SyntaxSpace(',');
                    writer.AppendFormatted(new Number(value));
                    break;

                case ALUOpARM.TST or ALUOpARM.TEQ or ALUOpARM.CMP or ALUOpARM.CMN:
                    writer.AppendFormatted(new Register(rn));
                    writer.SyntaxSpace(',');
                    writer.AppendFormatted(new Number(value));
                    break;

                case ALUOpARM.MOV or ALUOpARM.MVN:
                    writer.AppendFormatted(new Register(rd));
                    writer.SyntaxSpace(',');
                    writer.AppendFormatted(new Number(value));
                    break;

                default:
                    writer.AppendFormatted(new Register(rd));
                    writer.SyntaxSpace(',');
                    writer.AppendFormatted(new Register(rn));
                    writer.SyntaxSpace(',');
                    writer.AppendFormatted(new Number(value));
                    break;
            }
        }
        else
        {
            ShiftedOperand shifted = new(opcode & 0x0FFF);

            switch (op)
            {
                case ALUOpARM.TST or ALUOpARM.TEQ or ALUOpARM.CMP or ALUOpARM.CMN:
                    writer.AppendFormatted(new Register(rn));
                    writer.SyntaxSpace(',');
                    writer.AppendFormatted(shifted);
                    break;

                case ALUOpARM.MOV or ALUOpARM.MVN:
                    writer.AppendFormatted(new Register(rd));
                    writer.SyntaxSpace(',');
                    writer.AppendFormatted(shifted);
                    break;

                default:
                    writer.AppendFormatted(new Register(rd));
                    writer.SyntaxSpace(',');
                    writer.AppendFormatted(new Register(rn));
                    writer.SyntaxSpace(',');
                    writer.AppendFormatted(shifted);
                    break;
            }
        }

        return writer.Finalize();
    }
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

        bool hasOffset = HasOffset();

        if (preIndex && hasOffset)
            WriteOffset(ref writer);

        writer.Syntax(']');
        if (writeback) writer.Syntax('!');

        if (!preIndex && hasOffset)
            WriteOffset(ref writer);

        return writer.Finalize();


        bool HasOffset()
        {
            if (immediate)
                return new ShiftedOperand(data).IsMeaningful;
            else
                return data != 0;
        }

        void WriteOffset(ref TokenWriter writer)
        {
            writer.SyntaxSpace(',');

            if (immediate)
            {
                ShiftedOperand shifted = new(data);
                if (!add) writer.Syntax('-');
                writer.AppendFormatted(shifted);
            }
            else
                writer.AppendFormatted(new Number(data, negative: !add));
        }
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
        if (signed) writer.AppendFormatted(new Mnemonic("s"));
        writer.AppendFormatted(new Mnemonic(halfWord ? "h" : "b"));

        writer.BeginOperands();
        writer.AppendFormatted(new Register(rd));
        writer.SyntaxSpace(',');
        writer.Syntax('[');
        writer.AppendFormatted(new Register(rn));

        bool hasOffset = HasOffset();

        if (preIndex && hasOffset)
            WriteOffset(ref writer);

        writer.Syntax(']');
        if (writeback) writer.Syntax('!');

        if (!preIndex && hasOffset)
            WriteOffset(ref writer);

        return writer.Finalize();


        bool HasOffset()
        {
            if (immediate)
            {
                int imm = (int)(((opcode >> 4) & 0xF0) | (opcode & 0x0F));
                return imm != 0;
            }
            else
                return true;
        }

        void WriteOffset(ref TokenWriter writer)
        {
            writer.SyntaxSpace(',');

            if (immediate)
            {
                int imm = (int)(((opcode >> 4) & 0xF0) | (opcode & 0x0F));
                writer.AppendFormatted(new Number((uint)imm, negative: !add));
            }
            else
            {
                uint rm = opcode & 0x0F;
                if (!add) writer.Syntax('-');
                writer.AppendFormatted(new Register(rm));
            }
        }
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