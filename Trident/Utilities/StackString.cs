namespace Trident.Utilities
{
    internal ref struct StackString
    {
        private Span<char> _buffer;
        private int _length;

        internal StackString(Span<char> buffer)
        {
            _buffer = buffer;
            _length = 0;
        }

        internal readonly int Length => _length;
        internal ReadOnlySpan<char> AsSpan() => _buffer[.._length];


        internal void Append(char c) => _buffer[_length++] = c;

        internal void Append(ReadOnlySpan<char> text)
        {
            text.CopyTo(_buffer.Slice(_length));
            _length += text.Length;
        }

        internal void AppendFormatted<T>(T value, ReadOnlySpan<char> format = default)
            where T : ISpanFormattable
        {
            value.TryFormat(_buffer.Slice(_length), out int written, format, null);
            _length += written;
        }


        internal void PadRight(int totalWidth, char padChar = ' ')
        {
            int missing = totalWidth - _length;
            for (int i = 0; i < missing; i++)
                this += padChar;
        }


        public static StackString operator +(StackString s, char c)
        {
            s.Append(c);
            return s;
        }

        public static StackString operator +(StackString s, string text)
        {
            s.Append(text.AsSpan());
            return s;
        }

        public static StackString operator +(StackString s, ReadOnlySpan<char> text)
        {
            s.Append(text);
            return s;
        }


        public static StackString From(ReadOnlySpan<char> text, Span<char> buffer)
        {
            var s = new StackString(buffer);
            s.Append(text);
            return s;
        }
    }
}