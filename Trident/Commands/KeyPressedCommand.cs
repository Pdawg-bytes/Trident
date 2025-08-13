using Trident.Emulation;
using Trident.Core.Machine;
using Trident.Core.Hardware.Controller;

namespace Trident.Commands
{
    internal class KeyPressedCommand(GBAKey key, bool pressed) : EmulatorCommand
    {
        private readonly GBAKey _key = key;
        private readonly bool _pressed = pressed;

        public override void Execute(GBA gba) => gba.SetKeyState(_key, _pressed);
    }
}