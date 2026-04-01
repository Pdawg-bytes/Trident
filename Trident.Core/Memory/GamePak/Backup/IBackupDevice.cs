namespace Trident.Core.Memory.GamePak.Backup;

internal interface IBackupDevice : IDisposable
{
    BackupType Type { get; }
    uint Size       { get; }
    
    byte Read(uint address);
    void Write(uint address, byte value);
    
    void Reset();
    byte[] GetSaveData();
    void LoadSaveData(byte[] data);
}