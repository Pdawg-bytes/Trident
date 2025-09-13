using Trident.Emulation;
using Trident.Core.Machine;

namespace Trident.Commands
{
    internal readonly struct StepCommand(ulong cycles) : IEmulatorCommand
    {
        public void Execute(GBA gba, EmulatorThread thread) => gba.RunFor(cycles);
    }
}