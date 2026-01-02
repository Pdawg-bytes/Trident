using Trident.Core.Memory.MappedIO;

namespace Trident.Core.Hardware.IO;

internal readonly struct RegisterAccessor(Func<ushort> read, Action<ushort, WriteMask> write)
{
    internal readonly Func<ushort> Read = read;
    internal readonly Action<ushort, WriteMask> Write = write;
}