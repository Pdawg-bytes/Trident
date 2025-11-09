namespace Trident.Core.Debugging.Disassembly.Tokens
{
    public enum TokenType : byte
    {
        Opcode,
        Condition,
        Register,
        Shift,
        Number,
        PSR,
        Coprocessor,
        Syntax
    }
}