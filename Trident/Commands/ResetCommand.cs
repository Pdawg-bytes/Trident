using Trident.Emulation;
using Trident.Core.Machine;

namespace Trident.Commands;

internal readonly struct ResetCommand : IEmulatorCommand
{
    public void Execute(GBA gba, EmulatorThread thread) => gba.Reset();
}