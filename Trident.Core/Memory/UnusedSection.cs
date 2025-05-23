namespace Trident.Core.Memory
{
    internal class UnusedSection
    {
        internal UnusedSection() { }

        internal MemoryAccessHandler GetAccessHandler()
        {
            return new MemoryAccessHandler
            {
                Read8 = this.Read8,
                Read16 = this.Read16,
                Read32 = this.Read32,

                Write8 = this.Write8,
                Write16 = this.Write16,
                Write32 = this.Write32,

                Dispose = this.Dispose
            };
        }

        internal byte Read8(uint address) => byte.MaxValue;

        internal ushort Read16(uint address) => ushort.MaxValue;

        internal uint Read32(uint address) => uint.MaxValue;

        internal void Write8(uint address, byte value) { }

        internal void Write16(uint address, ushort value) { }

        internal void Write32(uint address, uint value) { }

        private void Dispose() { }
    }
}