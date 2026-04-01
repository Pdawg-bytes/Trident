namespace Trident.Core.Scheduling;

internal enum EventType
{
    PPU_HBlankStart,
    PPU_HBlankEnd,
    PPU_SetHBlankFlag,
    PPU_VBlankStart,
    PPU_VBlankEnd,
    PPU_VCounterMatch,

    APU_PSG1Generate,
    APU_PSG2Generate, 
    APU_PSG3Generate, 
    APU_PSG4Generate,
    APU_Sample,

    DMA_Activate,

    TMR_Overflow,

    EndOfQueue,
    Count
}