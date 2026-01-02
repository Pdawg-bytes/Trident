namespace Trident.Core.Hardware.DMA;

internal class DMAChannel(uint id)
{
    internal readonly uint ID = id;

    internal bool Enabled;
    internal bool Repeat;
    internal bool InterruptOnEnd;
    internal bool GamePakDRQ;

    internal uint Source;
    internal uint Destination;
    internal ushort TransferLength;

    internal TransferLatch Latch;

    internal DMATransferSize TransferSize;

    internal AddressingMode SourceControl;
    internal AddressingMode DestinationControl;

    internal DMAStartTiming StartTiming;
}

internal struct TransferLatch
{
    internal uint Source;
    internal uint Destination;
    
    // The internal latch can exceed 16-bit values when initialized from 0.
    internal uint TransferLength;

    internal uint BusValue;
}


public enum AddressingMode
{
    Increment = 0b00,
    Decrement = 0b01,
    Fixed     = 0b10,
    Reload    = 0b11
}

public enum DMAStartTiming
{
    Immediate = 0b00,
    VBlank    = 0b01,
    HBlank    = 0b10,
    Special   = 0b11
}

public enum DMATransferSize
{
    Half = 0b00,
    Word = 0b01
}