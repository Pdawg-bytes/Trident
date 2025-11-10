using Trident.Core.CPU;
using Trident.Core.Global;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Trident.Core.Debugging.Disassembly.Tokens
{
    #region Writer
    [InterpolatedStringHandler]
    internal ref struct TokenWriter
    {
        private Span<byte> _buffer;
        private int _offset;

        internal readonly int Written => _offset;
        internal int OperandsStartIndex { get; private set; }

        private bool _inOperands;

        public TokenWriter(int literalLength, int formattedCount, Span<byte> buffer, out bool success)
        {
            _buffer = buffer;
            _offset = 0;
            success = true;
        }

        public TokenWriter(Span<byte> buffer) => _buffer = buffer;

        internal WriteResult Finalize() => new(Written, OperandsStartIndex);



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
            if (s == " | ")
            {
                BeginOperands();
                return;
            }

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

        public void AppendFormatted(Mnemonic op)
        {
            if (op.Text.Length > 15)
                throw new ArgumentException("Token text too long (max 15 chars).");

            Write(MakeHeader(TokenType.Mnemonic, op.Text.Length, hasFlag: true));
            Write((byte)(op.IsCondition ? 1 : 0));

            for (int i = 0; i < op.Text.Length; i++)
                Write((byte)op.Text[i]);
        }

        public void AppendFormatted(Register reg)
        {
            if (reg.RegisterIndex > 15)
                throw new ArgumentException("Register index is out of range (max r15)");

            Write(MakeHeader(TokenType.Register, 1, hasFlag: false));
            Write((byte)(reg.RegisterIndex & 0x0F));
        }

        public void AppendFormatted(PSR psr)
        {
            Write(MakeHeader(TokenType.PSR, 1, hasFlag: false));
            Write((byte)((byte)psr.Flags | (psr.CPSR ? 0x80 : 0x00)));
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

        public void AppendFormatted(ShiftedOperand op)
        {
            AppendFormatted(op.Rm);

            if (op.ShiftAmount.Value == 0 && !op.RRX)
                return;

            SyntaxSpace(',');

            if (op.RRX)
            {
                AppendFormatted(new Mnemonic("rrx"));
                return;
            }

            ReadOnlySpan<char> text = op.Type switch
            {
                ShiftType.LSL => "lsl ",
                ShiftType.LSR => "lsr ",
                ShiftType.ASR => "asr ",
                ShiftType.ROR => "ror ",
                _ => "??? "
            };

            AppendFormatted(new Mnemonic(text));

            if (op.ByRegister) 
                AppendFormatted(op.Rs);
            else if (op.ShiftAmount.Value != 0)
                AppendFormatted(op.ShiftAmount);
        }

        public void AppendFormatted(Coprocessor coproc)
        {
            if (coproc.RegisterIndex > 15)
                throw new ArgumentException("Register/coprocessor index is out of range (max 15)");

            Write(MakeHeader(TokenType.Coprocessor, 1, hasFlag: false));
            Write((byte)((coproc.IsRegister ? 0x80 : 0x00) | (coproc.RegisterIndex & 0x0F)));
        }


        public void Syntax(char c)
        {
            Write(MakeHeader(TokenType.Syntax, 1, hasFlag: false));
            Write((byte)c);
        }

        public void SyntaxSpace(char c)
        {
            Syntax(c);
            Syntax(' ');
        }


        public void BeginOperands()
        {
            if (!_inOperands)
            {
                _inOperands = true;
                OperandsStartIndex = _offset;
            }
        }
    }

    internal static class WriterHost
    {
        public static WriteResult Write(Span<byte> buffer, [InterpolatedStringHandlerArgument("buffer")] TokenWriter handler) =>
            new(handler.Written, handler.OperandsStartIndex);
    }

    internal struct WriteResult(int bytesWritten, int operandsStartIndex)
    {
        internal int BytesWritten = bytesWritten;
        internal int OperandsStartIndex = operandsStartIndex;
    }
    #endregion


    #region Reader
    public ref struct TokenReader
    {
        private ReadOnlySpan<byte> _data;
        private int _offset;

        public readonly int Remaining => _data.Length - _offset;
        public readonly bool EndOfStream => _offset >= _data.Length;

        public TokenReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            _offset = 0;
        }

        public bool TryRead(out Token token)
        {
            if (_offset >= _data.Length)
            {
                token = default;
                return false;
            }

            byte header  = _data[_offset++];
            var type     = (TokenType)(header & 0x7);
            int length   = (header >> 3) & 0xF;
            bool hasFlag = (header & 0x80) != 0;

            int payloadLen = length + (hasFlag ? 1 : 0);

            token = new Token(type, length, hasFlag, _data.Slice(_offset, payloadLen));
            _offset += payloadLen;
            return true;
        }
    }

    public readonly ref struct Token
    {
        public readonly TokenType Type { get; }
        public readonly bool HasFlag { get; }
        public readonly int Length { get; }

        public readonly ReadOnlySpan<byte> Data { get; }

        public Token(TokenType type, int length, bool hasFlag, ReadOnlySpan<byte> data)
        {
            Type    = type;
            Length  = length;
            HasFlag = hasFlag;
            Data    = data;
        }
    }
    #endregion


    #region Token wrappers
    internal readonly ref struct Mnemonic(ReadOnlySpan<char> text, bool isCondition = false)
    {
        internal readonly ReadOnlySpan<char> Text = text;
        internal readonly bool IsCondition = isCondition;
    }

    internal readonly ref struct Number(uint value, bool negative = false, bool isLabel = false)
    {
        internal readonly uint Value = value;
        internal readonly bool Negative = negative;
        internal readonly bool IsLabel = isLabel;
    }


    internal readonly ref struct Register(uint registerIndex)
    {
        internal readonly uint RegisterIndex = registerIndex;
    }

    internal readonly ref struct Coprocessor(int registerIndex, bool isRegister)
    {
        internal readonly int RegisterIndex = registerIndex;
        internal readonly bool IsRegister = isRegister;
    }


    internal readonly ref struct PSR(bool cpsr, PSRFlags flags)
    {
        internal readonly bool CPSR = cpsr;
        internal readonly PSRFlags Flags = flags;
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


    internal readonly ref struct ShiftedOperand
    {
        internal readonly Register Rm;
        internal readonly ShiftType Type;
        internal readonly bool ByRegister;
        internal readonly Register Rs;
        internal readonly Number ShiftAmount;
        internal readonly bool RRX;

        public ShiftedOperand(uint shiftData)
        {
            Rm         = new Register(shiftData & 0xF);
            Type       = (ShiftType)((shiftData >> 5) & 0b11);
            ByRegister = shiftData.IsBitSet(4);

            if (ByRegister)
            {
                Rs          = new Register((shiftData >> 8) & 0xF);
                ShiftAmount = default;
                RRX         = false;
            }
            else
            {
                uint shamt = (shiftData >> 7) & 0x1F;
                if (shamt == 0 && (Type == ShiftType.LSR || Type == ShiftType.ASR))
                    shamt = 32;

                RRX         = (shamt == 0 && Type == ShiftType.ROR);
                ShiftAmount = new Number(shamt, negative: false);
                Rs          = default;
            }
        }
    }
    #endregion
}