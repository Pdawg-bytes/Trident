namespace Trident.Core.Debugging.Disassembly.Tokens
{
    internal enum TokenType : byte
    {
        Mnemonic,
        MnemonicSuffix,
        Condition,
        Register,
        Shift,
        Immediate,
        PSR,
        Syntax
    }
}