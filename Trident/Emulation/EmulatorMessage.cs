using Trident.Core.Machine;

namespace Trident.Emulation
{
    public interface IEmulatorCommand
    {
        void Execute(GBA gba, EmulatorThread thread);
    }
}