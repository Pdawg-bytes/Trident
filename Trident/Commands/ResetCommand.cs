using Trident.Emulation;
using Trident.Core.Machine;

namespace Trident.Commands
{
    internal class ResetCommand : EmulatorCommand
    {
        public override void Execute(GBA gba) => gba.Reset();
    }
}