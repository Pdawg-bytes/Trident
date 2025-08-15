using Trident.Emulation;
using Trident.Core.Machine;

namespace Trident.Commands
{
    internal enum LoadType { BIOS, GamePak }

    internal class LoadCommand(LoadType loadType, string path) : EmulatorCommand
    {
        private readonly LoadType _loadType = loadType;
        private readonly string _path = path;

        public override void Execute(GBA gba, EmulatorThread thread)
        {
            thread.SetPause(true);

            switch (_loadType)
            {
                case LoadType.BIOS:
                    gba.AttachBIOS(_path); break;
                case LoadType.GamePak:
                    gba.AttachGamePak(_path); break;
            }

            gba.Reset();
        }
    }
}