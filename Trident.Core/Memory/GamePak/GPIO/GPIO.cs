using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Enums;

namespace Trident.Core.Memory.GamePak.GPIO
{
    internal class GPIO
    {
        internal bool Readable { get; private set; }

        private byte _data;

        private List<GPIODevice> _devices;

        internal GPIODevice? GetDevice<T>() where T : GPIODevice 
            => _devices.Find(d => d is T);

        internal void AttachDevice(GPIODevice device) => 
            _devices.Append(device);

        internal int RemoveDevice<T>() where T : GPIODevice
            => _devices.RemoveAll(d => d is T);


        internal byte Read(uint address)
        {
            if (Readable) // Shouldn't ever hit, but just in case.
                return 0;

            switch ((GPIORegister)address)
            {
                case GPIORegister.Data:
                    byte value = 0;
                    // TODO: read data
                    return value;

                case GPIORegister.Direction: return 0; // TODO: return direction
                case GPIORegister.Control: return Readable ? (byte)1 : (byte)0;
            }

            return 0;
        }

        internal void Write(uint address, byte value)
        {
            // TODO: write
        }

        internal void Reset()
        {
            Readable = false;
            _data = 0;
        }
    }
}