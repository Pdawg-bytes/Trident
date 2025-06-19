namespace Trident.Core.Memory.GamePak.GPIO
{
    internal enum GPIODirection
    {
        In, // GPIO to GBA
        Out // GBA to GPIO
    }

    internal enum GPIORegister
    {
        Data = 0xC4,
        Direction = 0xC6,
        Control = 0xC8
    }
}