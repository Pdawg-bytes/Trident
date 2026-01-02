namespace Trident.Core.Debugging.Disassembly.Tokens;

public enum TokenType : byte
{
    Mnemonic,
    Condition,
    Register,
    Number,
    PSR,
    Coprocessor,
    Syntax,
    Unknown
}