namespace Trident.Core.Scheduling
{
    internal enum EventType
    {
        // PPU Events
        PPU_HBlankStart,
        PPU_HBlankEnd,
        PPU_SetHBlankFlag,
        PPU_VBlankStart,
        PPU_VBlankEnd,
        PPU_VCounterMatch,

        // APU Events
        APU_PSG1Generate,
        APU_PSG2Generate, 
        APU_PSG3Generate, 
        APU_PSG4Generate,
        APU_Sample,

        // DMA Events
        DMA_C0Activate,
        DMA_C1Activate,
        DMA_C2Activate,
        DMA_C3Activate,

        // Timer Events
        TMR_T0Overflow,
        TMR_T1Overflow,
        TMR_T2Overflow,

        EndOfQueue,
        Count
    }
}