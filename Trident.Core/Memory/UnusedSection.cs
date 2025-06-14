using Trident.Core.Enums;

namespace Trident.Core.Memory
{
    internal class UnusedSection
    {
        internal UnusedSection() { }

        internal MemoryAccessHandler GetAccessHandler()
        {
            return new MemoryAccessHandler
            (
                read8: this.Read8,
                read16: this.Read16,
                read32: this.Read32,

                write8: this.Write8,
                write16: this.Write16,
                write32: this.Write32,

                dispose: Dispose
            );
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