namespace Trident.Core.Hardware.IO
{
    internal readonly struct RegisterAccessor(Func<uint, ushort> read, Action<uint, bool, bool> write)
    {
        internal readonly Func<uint, ushort> Read = read;
        internal readonly Action<uint, bool, bool> Write = write;
    }
}