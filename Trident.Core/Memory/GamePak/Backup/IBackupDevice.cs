using Trident.Core.Enums;

namespace Trident.Core.Memory.GamePak.Backup
{
    internal interface IBackupDevice
    {
        uint Size { get; }
        BackupType Type { get; }

        void Reset();
        MemoryAccessHandler GetAccessHandler();
    }
}