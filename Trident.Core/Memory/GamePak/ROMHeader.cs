using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.GamePak;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 228)]
internal unsafe struct ROMHeader
{
    [FieldOffset(0)]   internal readonly uint EntryPoint;

    [FieldOffset(4)]   internal fixed byte NintendoBitmap[156];

    [FieldOffset(160)] internal GameData GameInfo;

    [FieldOffset(178)] internal byte Fixed96h;
    [FieldOffset(179)] internal byte UnitCode;
    [FieldOffset(180)] internal byte DeviceType;
    [FieldOffset(181)] internal fixed byte Reserved[7];
    [FieldOffset(188)] internal byte Version;
    [FieldOffset(189)] internal byte Checksum;
    [FieldOffset(190)] internal fixed byte Reserved2[2];

    [FieldOffset(192)] internal MultiBootHeader BootHeader;

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 18)]
    internal struct GameData
    {
        [FieldOffset(0)]  internal fixed byte Title[12];
        [FieldOffset(12)] internal fixed byte Code[4];
        [FieldOffset(16)] internal fixed byte Maker[2];
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 36)]
    internal struct MultiBootHeader
    {
        [FieldOffset(0)]  internal uint RAMEntryPoint;
        [FieldOffset(4)]  internal byte BootMode;
        [FieldOffset(5)]  internal byte SlaveID;
        [FieldOffset(6)]  internal fixed byte Unused[26];
        [FieldOffset(32)] internal uint JoyEntryPoint;
    }

    internal const int Size = 228;
}

internal static class ROMHeaderExtensions
{
    internal static unsafe bool IsValid(this ref ROMHeader header) => header.Fixed96h == 0x96 && header.CalculateChecksum() == header.Checksum;

    internal static unsafe byte CalculateChecksum(this ref ROMHeader header)
    {
        ReadOnlySpan<byte> data = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ROMHeader, byte>(ref header), ROMHeader.Size);

        byte chk = 0;
        for (int i = 0xA0; i <= 0xBC; i++)
            chk = (byte)(chk - data[i]);

        return (byte)((chk - 0x19) & 0xFF);
    }

    internal static unsafe (string title, string code, string maker) GetGameInfoStrings(this ref ROMHeader header)
    {
        ReadOnlySpan<byte> title = MemoryMarshal.CreateReadOnlySpan(ref header.GameInfo.Title[0], 12);
        ReadOnlySpan<byte> code  = MemoryMarshal.CreateReadOnlySpan(ref header.GameInfo.Code[0], 4);
        ReadOnlySpan<byte> maker = MemoryMarshal.CreateReadOnlySpan(ref header.GameInfo.Maker[0], 2);

        return (
            Encoding.ASCII.GetString(title).TrimEnd('\0'),
            Encoding.ASCII.GetString(code).TrimEnd('\0'),
            Encoding.ASCII.GetString(maker).TrimEnd('\0')
        );
    }
}