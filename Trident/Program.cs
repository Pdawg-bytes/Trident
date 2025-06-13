using Trident.Core.CPU;
using Trident.Core.Bus;
using Trident.Core.Enums;
using Trident.Core.Machine;

namespace Trident
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GBA gba = new();
            gba.AttachGamePak(@"D:\GBA_ROM\Metroid Fusion (USA)\Metroid Fusion (USA).gba");
            Console.WriteLine(gba.GetGamePakInfo());
        }
    }
}