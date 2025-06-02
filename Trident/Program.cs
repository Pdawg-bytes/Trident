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
            gba.AttachGamePak(@"C:\Users\pgago\Downloads\Super Monkey Ball Jr. (USA)\Super Monkey Ball Jr. (USA).gba");
        }
    }
}