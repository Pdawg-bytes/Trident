using Trident.Emulation;
using Trident.Core.Machine;
using Trident.Core.Hardware.Controller;

namespace Trident.Commands;

internal readonly struct KeyPressedCommand(GBAKey key, bool pressed)
{
    private readonly GBAKey _key = key;
    private readonly bool _pressed = pressed;

    public void Execute(GBA gba, EmulatorThread thread) => gba.SetKeyState(_key, _pressed);
}