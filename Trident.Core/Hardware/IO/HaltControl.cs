using Trident.Core.Global;

namespace Trident.Core.Hardware.IO
{
    internal class HaltControl(Action haltCPU)
    {
        private readonly Action _haltCPU = haltCPU;

        internal void Write(byte value)
        {
            if (((uint)value).IsBitSet(7))
            {
                // TODO: replace with log
                Console.WriteLine("HALTCNT bit 7 set");
            }
            else
                _haltCPU();
        }
    }
}