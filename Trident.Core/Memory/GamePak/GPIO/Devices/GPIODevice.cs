namespace Trident.Core.Memory.GamePak.GPIO
{
    public abstract class GPIODevice
    {
        private int _portDirections = 0;

        internal void SetDirections(int portDirections) => _portDirections = portDirections;
        internal GPIODirection GetDirection(int pin) => (GPIODirection)((_portDirections >> pin) & 1);

        public abstract void Reset();
        // We just use int here to make our lives easier. If C# didn't require us to cast everything,
        // I'd consider using a byte. The GPIO handler will handle the ORing & casting anyways.
        public abstract int Read();
        public abstract void Write(int value);
    }
}   