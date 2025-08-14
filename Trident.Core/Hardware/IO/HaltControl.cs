namespace Trident.Core.Hardware.IO
{
    internal class HaltControl(Action haltCPU, Func<uint> getPC)
    {
        private readonly Action _haltCPU = haltCPU;
        private readonly Func<uint> _getPC = getPC;

        internal void Write(byte value)
        {
            if (_getPC() > 0x3FFF) return;

            if ((value & 0x80) != 0)
            {
                // TODO: replace with log
                Console.WriteLine("HALTCNT bit 7 set");
            }
            else
                _haltCPU();
        }
    }
}