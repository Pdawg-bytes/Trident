using Trident.Core.CPU;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Trident.Core.Debugging.Disassembly.Tokens
{
    [InterpolatedStringHandler]
    internal ref struct TokenWriterHandler
    {
        private Span<byte> _buffer;
        private int _offset;

        public TokenWriterHandler(int literalLength, int formattedCount, Span<byte> buffer, out bool success)
        {
            _buffer = buffer;
            _offset = 0;
            success = true;
        }

        public readonly int Written => _offset;

        private byte MakeHeader(TokenType type, int length, bool hasFlag)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((int)type, 7);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 15);

            return (byte)(((hasFlag ? 1 : 0) << 7) | ((length & 0xF) << 3) | ((int)type & 0x7));
        }

        private void Write(byte data)
        {
            if (_offset >= _buffer.Length)
                throw new InvalidOperationException("Buffer overflow in Write.");

            _buffer[_offset++] = data;
        }


        public void AppendLiteral(ReadOnlySpan<char> s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '#':
                    case ',':
                    case '[':
                    case ']':
                    case '{':
                    case '}':
                    case ' ':
                    case '-':
                    case '!':
                    case '^':
                        Syntax(c);
                        break;

                    default:
                        throw new Exception($"Token literal contains invalid character '{c}'.");
                }
            }
        }

        public void AppendFormatted(Opcode op)
        {
            if (op.Text.Length > 15)
                throw new ArgumentException("Token text too long (max 15 chars).");

            Write(MakeHeader(TokenType.Opcode, op.Text.Length, hasFlag: true));
            Write((byte)(op.IsCondition ? 1 : 0));

            for (int i = 0; i < op.Text.Length; i++)
                Write((byte)op.Text[i]);
        }

        public void AppendFormatted(Register reg)
        {
            if (reg.RegisterIndex > 15)
                throw new ArgumentException("Register index is out of range (max r15)");

            Write(MakeHeader(TokenType.Register, 1, hasFlag: false));
            Write((byte)reg.RegisterIndex);
        }

        public void AppendFormatted(PSR psr)
        {
            Write(MakeHeader(TokenType.PSR, 2, hasFlag: true));
            Write((byte)(psr.CPSR ? 1 : 0));
            Write((byte)psr.Flags);
        }

        public void AppendFormatted(Number num)
        {
            Write(MakeHeader(TokenType.Number, 4, hasFlag: true));

            Write((byte)
            (
                (num.Negative ? 1 : 0) |
                ((num.IsLabel ? 1 : 0) << 1)
            ));

            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(_offset, 4), num.Value);
            _offset += 4;
        }

        public void AppendFormatted(Shift shift)
        {
            Write(MakeHeader(TokenType.Shift, 2, hasFlag: false));
            Write((byte)shift.Type);
        }

        public void AppendFormatted(Coprocessor coproc)
        {
            if (coproc.RegisterIndex > 15)
                throw new ArgumentException("Register/coprocessor index is out of range (max 15)");

            Write(MakeHeader(TokenType.Coprocessor, 2, hasFlag: true));
            Write((byte)(coproc.IsRegister ? 1 : 0));
            Write((byte)coproc.RegisterIndex);
        }


        public void Syntax(char c)
        {
            Write(MakeHeader(TokenType.Syntax, 1, hasFlag: false));
            Write((byte)c);
        }
    }


    internal static class WriterHost
    {
        public static int Write(Span<byte> buffer, [InterpolatedStringHandlerArgument("buffer")] TokenWriterHandler handler) =>
            handler.Written;
    }


    #region Token wrappers
    internal readonly ref struct Opcode(ReadOnlySpan<char> text, bool isCondition = false)
    {
        public readonly ReadOnlySpan<char> Text = text;
        public readonly bool IsCondition = isCondition;
    }


    internal readonly ref struct Register(int registerIndex)
    {
        public readonly int RegisterIndex = registerIndex;
    }

    internal readonly ref struct Coprocessor(int registerIndex, bool isRegister)
    {
        public readonly int RegisterIndex = registerIndex;
        public readonly bool IsRegister = isRegister;
    }


    internal readonly ref struct PSR(bool cpsr, PSRFlags flags)
    {
        public readonly bool CPSR = cpsr;
        public readonly PSRFlags Flags = flags;
    }

    [Flags]
    public enum PSRFlags : uint
    {
        None = 0,
        F = 1 << 0,
        S = 1 << 1,
        X = 1 << 2,
        C = 1 << 3
    }


    internal readonly ref struct Shift(ShiftType type)
    {
        public readonly ShiftType Type = type;
    }

    internal readonly ref struct Number(uint value, bool signed = false, bool isLabel = false)
    {
        public readonly uint Value = value;
        public readonly bool Negative = signed;
        public readonly bool IsLabel = isLabel;
    }
    #endregion
}