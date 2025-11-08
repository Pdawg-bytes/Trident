using System.Buffers.Binary;

namespace Trident.Core.Debugging.Disassembly.Tokens
{
    internal ref struct TokenWriter
    {
        private Span<byte> _buffer;
        private int _offset;

        public TokenWriter(Span<byte> buffer)
        {
            _buffer = buffer;
            _offset = 0;
        }

        public int Written => _offset;


        private byte MakeHeader(TokenType type, int length, bool hasFlag) =>
            (byte)(((hasFlag ? 1 : 0) << 7) | ((length & 0x7) << 4) | ((int)type & 0xF));

        private void WriteSpan(TokenType type, ReadOnlySpan<char> text)
        {
            if (text.Length > 8)
                throw new ArgumentException("Token text too long (max 8 chars).");

            _buffer[_offset++] = MakeHeader(type, text.Length, hasFlag: false);
            for (int i = 0; i < text.Length; i++)
                _buffer[_offset++] = (byte)text[i];
        }


        public void Mnemonic(ReadOnlySpan<char> text)
            => WriteSpan(TokenType.Mnemonic, text);

        public void MnemonicSuffix(ReadOnlySpan<char> text)
            => WriteSpan(TokenType.MnemonicSuffix, text);

        public void PSR(ReadOnlySpan<char> text)
            => WriteSpan(TokenType.PSR, text);


        public void Register(int regIndex)
        {
            string alias = _registers[regIndex];
            WriteSpan(TokenType.Register, alias);
        }

        public void Condition(int condId)
        {
            _buffer[_offset++] = MakeHeader(TokenType.Condition, 1, hasFlag: false);
            _buffer[_offset++] = (byte)condId;
        }

        public void Shift(int shiftId)
        {
            _buffer[_offset++] = MakeHeader(TokenType.Shift, 1, hasFlag: false);
            _buffer[_offset++] = (byte)shiftId;
        }

        public void Immediate(uint value, bool signed = false)
        {
            _buffer[_offset++] = MakeHeader(TokenType.Immediate, 4, hasFlag: true);
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(_offset, 4), value);
            _offset += 4;
            _buffer[_offset++] = (byte)(signed ? 1 : 0);
        }

        public void Syntax(char c, bool space = false)
        {
            _buffer[_offset++] = MakeHeader(TokenType.Syntax, space ? 2 : 1, hasFlag: false);
            _buffer[_offset++] = (byte)c;
            _buffer[_offset++] = (byte)' ';
        }


        private static readonly string[] _registers = [ "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12", "sp", "lr", "pc" ];
    }
}