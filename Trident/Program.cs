using Trident.Core.CPU;
using Trident.Core.Bus;
using Trident.Core.Enums;

namespace Trident
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ARM7TDMI arm = new(new DataBus());
        }
    }
}