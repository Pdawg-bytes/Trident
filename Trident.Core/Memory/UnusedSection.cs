using Trident.Core.CPU.Pipeline;
using Trident.Core.Memory.Region;

namespace Trident.Core.Memory
{
    internal class UnusedSection(Action<uint> step) : IMemoryRegion
    {
        private readonly Action<uint> _step = step;


        public byte Read8(uint address, PipelineAccess access)
        {
            _step(1);

            // TODO: open bus
            return 0xFF;
        }

        public ushort Read16(uint address, PipelineAccess access)
        {
            _step(1);

            // TODO: open bus
            return 0xFFFF;
        }

        public uint Read32(uint address, PipelineAccess access)
        {
            _step(1);

            // TODO: open bus
            return 0xFFFFFFFF;
        }

        public void Write8(uint address, PipelineAccess access, byte value)    => _step(1);
        public void Write16(uint address, PipelineAccess access, ushort value) => _step(1);
        public void Write32(uint address, PipelineAccess access, uint value)   => _step(1);

        public void Dispose() { }
    }
}