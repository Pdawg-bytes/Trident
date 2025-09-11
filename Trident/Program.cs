using Trident.Windowing;
using Trident.Core.Machine;

namespace Trident
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GBA gba = new();

            using var window = new EmulatorWindow(gba);
            window.Run();

            gba.Dispose();
        }
    }
}