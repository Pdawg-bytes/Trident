namespace Trident.Core.Memory
{
    public readonly struct DebugMemoryRead<T>(T value, uint baseAddr, uint endAddr, bool isValid) where T : unmanaged
    {
        public readonly T Value = value;
        public readonly uint BaseAddress = baseAddr;
        public readonly uint EndAddress = endAddr;
        public readonly bool IsValid = isValid;
    }
}