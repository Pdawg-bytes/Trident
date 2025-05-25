using Trident.Core.Enums;

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

        internal byte Read8(uint address, PipelineAccess access) => byte.MaxValue;

        internal ushort Read16(uint address, PipelineAccess access) => ushort.MaxValue;

        internal uint Read32(uint address, PipelineAccess access) => uint.MaxValue;

        internal void Write8(uint address, PipelineAccess access, byte value) { }

        internal void Write16(uint address, PipelineAccess access, ushort value) { }

        internal void Write32(uint address, PipelineAccess access, uint value) { }

        private void Dispose() { }
    }
}