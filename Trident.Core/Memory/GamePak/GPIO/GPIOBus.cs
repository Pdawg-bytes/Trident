namespace Trident.Core.Memory.GamePak.GPIO
{
    internal class GPIOBus
    {
        internal bool Readable { get; private set; }

        private byte _data;
        private byte _directions;

        private List<GPIODevice> _devices;

        internal T? GetDevice<T>() where T : GPIODevice 
            => (T?)_devices.Find(d => d is T);

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

                    foreach (var device in _devices)
                        value |= (byte)device.Read();

                    _data &= _directions;                           // Keep output bits
                    _data |= (byte)(value & (~_directions & 0x0F)); // Update input bits
                    return value;

                case GPIORegister.Direction: return _directions;
                case GPIORegister.Control: return Readable ? (byte)1 : (byte)0;
            }

            return 0;
        }

        internal void Write(uint address, byte value)
        {
            switch ((GPIORegister)address)
            {
                case GPIORegister.Data:
                    _data &= (byte)~_directions;          // Keep input bits
                    _data |= (byte)(value & _directions); // Set new output bits

                    foreach (var device in _devices)
                        device.Write(_data);
                    break;
                case GPIORegister.Direction:
                    _directions = (byte)(value & 0x0F);

                    foreach (var device in _devices)
                        device.SetDirections(_directions);
                    break;
                case GPIORegister.Control: Readable = (value & 1) != 0; break;
            }
        }

        internal void Reset()
        {
            Readable = false;
            _data = 0;
            _directions = 0;
            foreach (var device in _devices)
            {
                device.Reset();
                device.SetDirections(_directions);
            }
        }
    }
}