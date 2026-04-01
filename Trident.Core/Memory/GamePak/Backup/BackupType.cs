namespace Trident.Core.Memory.GamePak.Backup;

[Flags]
public enum BackupType
{
    None = 0,
    
    SRAM = 1 << 0,
    
    EEPROM512B   = 1 << 2, // 4Kbit
    EEPROM8K     = 1 << 3, // 64Kbit
    EEPROMDetect = 1 << 4,
    
    Flash64K  = 1 << 5, // 512Kbit
    Flash128K = 1 << 6, // 1Mbit
}

public static class BackupTypeExtensions
{
    public static uint GetSize(this BackupType type)
    {
        return type switch
        {
            BackupType.SRAM       => 32 * 1024,
            BackupType.EEPROM512B => 512,
            BackupType.EEPROM8K   => 8 * 1024,
            BackupType.Flash64K   => 64 * 1024,
            BackupType.Flash128K  => 128 * 1024,
            _ => 0
        };
    }

    public static bool IsEEPROM(this BackupType type) => (type & (BackupType.EEPROM512B | BackupType.EEPROM8K | BackupType.EEPROMDetect)) != 0;
    public static bool IsFlash(this BackupType type)  => (type & (BackupType.Flash64K   | BackupType.Flash128K)) != 0;
}