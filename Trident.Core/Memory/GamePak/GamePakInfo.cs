using Trident.Core.Memory.GamePak.Backup;

namespace Trident.Core.Memory.GamePak;

public readonly record struct GamePakInfo
{
    public string Title { get; init; }
    public string Code  { get; init; }
    public string Maker { get; init; }
    
    public uint Size { get; init; }

    public BackupType BackupType { get; init; }
    public uint BackupSize { get; init; }

    public uint AddressMask { get; init; }
}