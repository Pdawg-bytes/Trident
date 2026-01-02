namespace Trident.Core.Memory.GamePak.Backup;

internal interface IBackupDevice
{
    uint Size { get; }
    BackupType Type { get; }

    void Reset();
    byte Read(uint address);
    void Write(uint address, byte value);

    void Dispose();
}