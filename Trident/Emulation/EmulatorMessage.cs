using Trident.Core.Machine;

namespace Trident.Emulation
{
    public abstract class EmulatorCommand
    {
        public abstract void Execute(GBA gba, EmulatorThread thread);
    }
}