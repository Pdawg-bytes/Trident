namespace Trident.Core.Memory.GamePak.Backup;

public enum BackupType
{
    None         = 1 << 0,
    SRAM         = 1 << 1,
    Flash64K     = 1 << 2,
    Flash128K    = 1 << 3,
    EEPROMDetect = 1 << 4,
    EEPROM512B   = 1 << 5,
    EEPROM8K     = 1 << 6
}