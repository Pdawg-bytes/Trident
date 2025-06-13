using Trident.Core.Enums;

namespace Trident.Core.Memory.GamePak
{
    public record class GamePakInfo
    {
        public string Title { get; init; }
        public string Code { get; init; }
        public string Maker { get; init; }
        
        public uint Size { get; init; }

        public BackupType BackupType { get; init; }
        public uint BackupSize { get; init; }
    }
}