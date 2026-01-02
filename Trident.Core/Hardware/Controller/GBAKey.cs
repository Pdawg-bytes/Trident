namespace Trident.Core.Hardware.Controller;

[Flags]
public enum GBAKey : ushort
{
    A      = 0,
    B      = 1,
    Select = 2,
    Start  = 3,
    Right  = 4,
    Left   = 5,
    Up     = 6,
    Down   = 7,
    RB     = 8,
    LB     = 9
}