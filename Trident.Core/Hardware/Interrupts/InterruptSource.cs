namespace Trident.Core.Hardware.Interrupts;

internal enum InterruptSource
{
    LCD_VBlank          = 1 << 0,
    LCD_HBlank          = 1 << 1,
    LCD_VCounterMatch   = 1 << 2,

    Timer               = 1 << 3,

    Serial_DataTransfer = 1 << 7,

    DMA                 = 1 << 8,

    Keypad              = 1 << 12,

    GamePak             = 1 << 13,
}