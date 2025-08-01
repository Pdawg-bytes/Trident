using Trident.Core.Machine;
using Trident.Windowing;

namespace Trident
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            GBA gba = new();
            gba.AttachGamePak(@"D:\GBA_ROM\Metroid Fusion (USA)\Metroid Fusion (USA).gba");

            using var window = new EmulatorWindow(gba);
            window.Run();

            gba.Dispose();
        }
    }
}