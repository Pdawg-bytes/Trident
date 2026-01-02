using Trident.Core.Global;
using Trident.CodeGeneration.Shared;
using System.Runtime.CompilerServices;
using Trident.Core.Debugging.Disassembly.Tokens;

using static Trident.Core.Debugging.Disassembly.DisassemblerUtilities;

namespace Trident.Core.Debugging.Disassembly;

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
    internal static DisassembledInstruction Disassemble(uint address, uint lr, ushort opcode, byte[] tokenBuffer) =>
        Disassemble(address, lr, opcode, ThumbDecoder.DetermineThumbGroup(opcode), tokenBuffer);

    internal static DisassembledInstruction Disassemble(uint address, uint lr, ushort opcode, ThumbGroup group, byte[] tokenBuffer)
    {
        WriteResult result = group switch
        {
            ThumbGroup.ShiftImmediate      => ShiftImmediate(opcode, tokenBuffer),
            ThumbGroup.AddSubtract         => AddSubtract(opcode, tokenBuffer),
            ThumbGroup.ImmediateOperations => ImmediateOperations(opcode, tokenBuffer),
            ThumbGroup.ThumbALU            => DataProcessing(opcode, tokenBuffer),
            ThumbGroup.HiRegisterOps       => HighRegister(opcode, tokenBuffer),
            ThumbGroup.LoadPCRelative      => LoadPCRelative(opcode, tokenBuffer),
            ThumbGroup.LoadStoreRegOffset  => LoadStoreRegOrSigned(opcode, false, tokenBuffer),
            ThumbGroup.LoadStoreSigned     => LoadStoreRegOrSigned(opcode, true, tokenBuffer),
            ThumbGroup.LoadStoreImmOffset  => LoadStoreImmOffset(opcode, tokenBuffer),
            ThumbGroup.LoadStore16         => LoadStore16(opcode, tokenBuffer),
            ThumbGroup.LoadStoreSPRelative => LoadStoreSPRelative(opcode, tokenBuffer),
            ThumbGroup.LoadAddress         => LoadAddress(opcode, address, tokenBuffer),
            ThumbGroup.AddOffsetSP         => AddOffsetSP(opcode, tokenBuffer),
            ThumbGroup.PushPop             => PushPop(opcode, tokenBuffer),
            ThumbGroup.LoadStoreMultiple   => LoadStoreMultiple(opcode, tokenBuffer),
            ThumbGroup.ConditionalBranch   => ConditionalBranch(opcode, address, tokenBuffer),
            ThumbGroup.SoftwareInterrupt   => SoftwareInterrupt(opcode, tokenBuffer),
            ThumbGroup.UnconditionalBranch => UnconditionalBranch(opcode, address, tokenBuffer),
            ThumbGroup.LongBranchWithLink  => LongBranchWithLink(opcode, lr, tokenBuffer),
            ThumbGroup.BranchExchange      => BranchExchange(opcode, tokenBuffer),

            _ => EmitUnknownInstruction(tokenBuffer)
        };

        return new DisassembledInstruction
        {
            Address = address,
            Opcode = opcode,

            Tokens = new ReadOnlyMemory<byte>(tokenBuffer, 0, result.BytesWritten),
            OperandsStartIndex = result.OperandsStartIndex
        };
    }


    #region Shift
    private static WriteResult ShiftImmediate(uint opcode, Span<byte> buffer)
    {
        Mnemonic op = new(_shiftMnemonics[(opcode >> 11) & 0b11]);
        Register rd = new(opcode & 0b111);
        Register rs = new((opcode >> 3) & 0b111);
        uint shamt  = (opcode >> 6) & 0x1F;

        return WriterHost.Write(buffer, $"{op} | {rd}, {rs}, {new Number(shamt, hexFormat: false)}");
    }
    #endregion


    #region Arithmetic
    private static WriteResult AddSubtract(uint opcode, Span<byte> buffer)
    {
        Mnemonic op    = new(opcode.IsBitSet(9) ? "sub" : "add");
        Register rd    = new(opcode & 0b111);
        Register rs    = new((opcode >> 3) & 0b111);
        uint data      = (opcode >> 6) & 0x1F;
        bool immediate = opcode.IsBitSet(10);

        if (immediate)
            return WriterHost.Write(buffer, $"{op} | {rd}, {rs}, {new Number(data)}");
        else
            return WriterHost.Write(buffer, $"{op} | {rd}, {rs}, {new Register(data)}");
    }

    private static WriteResult LoadAddress(uint opcode, uint address, Span<byte> buffer)
    {
        Number offset = new((opcode & 0xFF) << 2);
        Register rd   = new((opcode >> 8) & 0b111);
        Register rs   = new(opcode.IsBitSet(11) ? 13u : 15u);

        return WriterHost.Write(buffer, $"{new Mnemonic("add")} | {rd}, {rs}, {offset}");
    }

    private static WriteResult AddOffsetSP(uint opcode, Span<byte> buffer)
    {
        Number offset = new((opcode & 0x3F) << 2, negative: opcode.IsBitSet(7));
        return WriterHost.Write(buffer, $"{new Mnemonic("add")} | {new Register(13)}, {offset}");
    }
    #endregion


    #region Data processing
    private static WriteResult DataProcessing(uint opcode, Span<byte> buffer)
    {
        Register rd = new(opcode & 0b111);
        Register rs = new((opcode >> 3) & 0b111);
        Mnemonic op = new(_aluMnemonics[(opcode >> 6) & 0x0F]);

        return WriterHost.Write(buffer, $"{op} | {rd}, {rs}");
    }
    #endregion


    #region Load/Store
    private static WriteResult LoadPCRelative(uint opcode, Span<byte> buffer)
    {
        Number offset = new((byte)opcode);
        Register rd   = new((opcode >> 8) & 0b111);

        return WriterHost.Write(buffer, $"{new Mnemonic("ldr")} | {rd}, [{new Register(15)}, {offset}]");
    }

    private static WriteResult LoadStoreRegOrSigned(uint opcode, bool signed, Span<byte> buffer)
    {
        Register rd = new(opcode & 0b111);
        Register rb = new((opcode >> 3) & 0b111);
        Register ro = new((opcode >> 6) & 0b111);
        uint op = (opcode >> 10) & 0b11;

        Mnemonic mnemonic = new
        (
            signed
                ? _loadStoreSignedMnemonics[op]
                : _loadStoreRegMnemonics[op]
        );

        return WriterHost.Write(buffer, $"{mnemonic} | {rd}, [{rb}, {ro}]");
    }

    private static WriteResult LoadStoreImmOffset(uint opcode, Span<byte> buffer)
    {
        Mnemonic op = new(_loadStoreImmOffMnemonics[(opcode >> 11) & 0b11]);
        Register rd = new(opcode & 0b111);
        Register rb = new((opcode >> 3) & 0b111);

        uint offset = (opcode >> 6) & 0x1F;
        offset <<= (int)(~opcode & 0b11u);

        return WriterHost.Write(buffer, $"{op} | {rd}, [{rb}, {new Number(offset)}]");
    }

    private static WriteResult LoadStore16(uint opcode, Span<byte> buffer)
    {
        Mnemonic op   = new(opcode.IsBitSet(11) ? "ldrh" : "strh");
        Register rd   = new(opcode & 0b111);
        Register rb   = new((opcode >> 3) & 0b111);
        Number offset = new(((opcode >> 6) & 0x1F) << 1);

        return WriterHost.Write(buffer, $"{op} | {rd}, [{rb}, {offset}]");
    }

    private static WriteResult LoadStoreSPRelative(uint opcode, Span<byte> buffer)
    {
        Mnemonic op   = new(opcode.IsBitSet(11) ? "ldr" : "str");
        Register rd   = new((opcode >> 8) & 0b111);
        Number offset = new((opcode & 0xFF) << 2);

        return WriterHost.Write(buffer, $"{op} | {rd}, [{new Register(13)}, {offset}]");
    }
    #endregion


    #region Miscellaneous
    private static WriteResult ImmediateOperations(uint opcode, Span<byte> buffer)
    {
        Mnemonic op      = new(_immediateOpMnemonics[(opcode >> 11) & 0b11]);
        Number immediate = new((byte)opcode);
        Register rd      = new((opcode >> 8) & 0b111);

        return WriterHost.Write(buffer, $"{op} | {rd}, {immediate}");
    }

    private static WriteResult HighRegister(uint opcode, Span<byte> buffer)
    {
        Register rd = new(((opcode >> 0) & 0b111) | (opcode.IsBitSet(7) ? 8 : 0u));
        Register rs = new(((opcode >> 3) & 0b111) | (opcode.IsBitSet(6) ? 8 : 0u));
        Mnemonic op = new(_highRegisterMnemonics[(opcode >> 8) & 0b11]);

        return WriterHost.Write(buffer, $"{op} | {rd}, {rs}");
    }
    #endregion


    #region Block transfer
    private static WriteResult PushPop(uint opcode, Span<byte> buffer)
    {
        uint regList = (byte)opcode;
        uint rBit    = (opcode >> 8) & 1;
        bool pop     = opcode.IsBitSet(11);

        regList |= rBit << (pop ? 15 : 14);

        TokenWriter writer = new(buffer);

        writer.AppendFormatted(new Mnemonic(pop ? "pop" : "push"));

        writer.BeginOperands();
        AppendRegisterList(ref writer, regList, userMode: false);

        return writer.Finalize();
    }

    private static WriteResult LoadStoreMultiple(uint opcode, Span<byte> buffer)
    {
        uint regList = (byte)opcode;
        Register rb  = new((opcode >> 8) & 0b111);
        bool load    = opcode.IsBitSet(11);

        TokenWriter writer = new(buffer);

        writer.AppendFormatted(new Mnemonic(load ? "ldmia" : "stmia"));

        writer.BeginOperands();
        writer.AppendFormatted(rb);
        writer.Syntax('!');
        writer.SyntaxSpace(',');
        AppendRegisterList(ref writer, regList, userMode: false);

        return writer.Finalize();
    }
    #endregion


    #region Software interrupt
    private static WriteResult SoftwareInterrupt(uint opcode, Span<byte> buffer) => 
        WriterHost.Write(buffer, $"{new Mnemonic("swi")} | {new Number(opcode & 0xFF)}");
    #endregion


    #region Branch
    private static WriteResult ConditionalBranch(uint opcode, uint address, Span<byte> buffer)
    {
        uint offset   = (uint)(opcode & 0xFF).ExtendFrom(8) << 1;
        Number target = new(address + offset + 4, isLabel: true);

        WriteResult result = WriterHost.Write(buffer, $"{new Mnemonic("b")} | {target}");
        InsertConditionMnemonic((opcode >> 8) & 0x0F, buffer, ref result);
        return result;
    }

    private static WriteResult UnconditionalBranch(uint opcode, uint address, Span<byte> buffer)
    {
        uint offset = (uint)(opcode & 0x07FF).ExtendFrom(11) << 1;
        Number target = new((address + offset) + 4, isLabel: true);

        return WriterHost.Write(buffer, $"{new Mnemonic("b")} | {target}");
    }

    private static WriteResult LongBranchWithLink(uint opcode, uint lr, Span<byte> buffer)
    {
        uint offset          = (opcode & 0x07FF) << 1;
        bool completesBranch = opcode.IsBitSet(11);
        Number target        = new(lr + offset, isLabel: true);

        Mnemonic mnemonic = new(completesBranch ? "blh" : "bll");

        if (completesBranch)
            return WriterHost.Write(buffer, $"{mnemonic} | {target}");
        else
            return WriterHost.Write(buffer, $"{mnemonic}");
    }

    private static WriteResult BranchExchange(uint opcode, Span<byte> buffer) =>
        WriterHost.Write(buffer, $"{new Mnemonic("bx")} | {new Register(((opcode >> 3) & 0b111) | (opcode.IsBitSet(6) ? 8 : 0u))}");
    #endregion
}