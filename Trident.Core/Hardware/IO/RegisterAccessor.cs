namespace Trident.Core.Hardware.IO
{
    internal readonly struct RegisterAccessor(Func<ushort> read, Action<ushort, bool, bool> write)
    {
        internal readonly Func<ushort> Read = read;
        internal readonly Action<ushort, bool, bool> Write = write;
    }
}