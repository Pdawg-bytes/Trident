namespace Trident.Core.Debugging.Disassembly
{
    public enum TokenKind : byte
    {
        Mnemonic, Condition, Register, Immediate, Label,
        Shift, Symbol, MemoryStart, MemoryEnd, Text
    }

    public readonly struct Token(TokenKind k, long data)
    {
        public readonly TokenKind Kind = k;
        public readonly long Data = data;
    }


    internal static class TokenFormatter
    {
        internal static Token Mnemonic(ReadOnlySpan<char> s)  => new(TokenKind.Mnemonic, Pack8(s));
        internal static Token Condition(ReadOnlySpan<char> s) => new(TokenKind.Condition, Pack8(s));
        internal static Token Shift(ReadOnlySpan<char> s)     => new(TokenKind.Shift, Pack8(s));
        internal static Token Text(ReadOnlySpan<char> s)      => new(TokenKind.Text, Pack8(s));
        internal static Token Symbol(char c)                  => new(TokenKind.Symbol, c);

        internal static Token Register(int r)     => new(TokenKind.Register, r);

        internal static Token Immediate(uint val) => new(TokenKind.Immediate, val);
        internal static Token Immediate(int val)  => new(TokenKind.Immediate, val);

        internal static Token Label(uint address) => new(TokenKind.Label, address);

        internal static Token MemoryStart()       => new(TokenKind.MemoryStart, 0);
        internal static Token MemoryEnd()         => new(TokenKind.MemoryEnd, 0);

        internal static long Pack8(ReadOnlySpan<char> s)
        {
            long val = 0;
            int len = Math.Min(8, s.Length);

            for (int i = 0; i < len; i++)
                val |= (byte)(s[i] << (i * 8));

            return val;
        }
    }
}