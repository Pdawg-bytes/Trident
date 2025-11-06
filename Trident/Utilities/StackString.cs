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
        internal ReadOnlySpan<char> AsSpan() => _buffer.Slice(0, _length);


        internal void Append(char c)
        {
            if (_length < _buffer.Length - 1)
                _buffer[_length++] = c;
        }

        internal void Append(ReadOnlySpan<char> text)
        {
            int available = _buffer.Length - _length;
            int toCopy = Math.Min(text.Length, available);
            text[..toCopy].CopyTo(_buffer.Slice(_length));
            _length += toCopy;
        }

        internal void AppendFormatted<T>(T value, ReadOnlySpan<char> format = default)
            where T : ISpanFormattable
        {
            if (value.TryFormat(_buffer.Slice(_length, _buffer.Length - _length - 1), out int written, format, null))
                _length += written;
        }

        internal void AppendBinary(uint value, int width = 32)
        {
            for (int i = width - 1; i >= 0; i--)
                Append(((value >> i) & 1) == 1 ? '1' : '0');
        }


        internal void PadLeft(int totalWidth, char padChar = ' ')
        {
            int missing = totalWidth - _length;
            if (missing <= 0) return;

            if (_length + missing >= _buffer.Length)
                missing = _buffer.Length - _length - 1;

            for (int i = _length - 1; i >= 0; i--)
                _buffer[i + missing] = _buffer[i];

            for (int i = 0; i < missing; i++)
                _buffer[i] = padChar;

            _length += missing;
        }

        internal void PadRight(int totalWidth, char padChar = ' ')
        {
            int missing = totalWidth - _length;
            for (int i = 0; i < missing && _length < _buffer.Length - 1; i++)
                Append(padChar);
        }


        public static StackString operator +(StackString s, char c)
        {
            s.Append(c);
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