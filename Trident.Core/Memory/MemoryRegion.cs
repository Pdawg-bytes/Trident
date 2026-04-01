namespace Trident.Core.Memory;

public enum MemoryRegion
{
    BIOS = 0,

    EWRAM = 2,
    IWRAM = 3,

    MMIO = 4,

    PRAM = 5,
    VRAM = 6,
    OAM = 7,

    GamePak = 8,
    Backup = 14
}