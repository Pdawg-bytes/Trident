using Trident.Core.CPU.Pipeline;

namespace Trident.Core.Memory
{
    internal class UnusedSection(Action<uint> step)
    {
        private Action<uint> _step = step;


        internal MemoryAccessHandler GetAccessHandler()
        {
            return new MemoryAccessHandler
            (
                read8:  Read8,
                read16: Read16,
                read32: Read32,

                write8:  (_, _, _) => _step(1),
                write16: (_, _, _) => _step(1),
                write32: (_, _, _) => _step(1),

                dispose: null
            );
        }

        internal byte Read8(uint address, PipelineAccess access)
        {
            _step(1);

            // TODO: open bus
            return 0xFF;
        }

        internal ushort Read16(uint address, PipelineAccess access)
        {
            _step(1);

            // TODO: open bus
            return 0xFFFF;
        }

        internal uint Read32(uint address, PipelineAccess access)
        {
            _step(1);

            // TODO: open bus
            return 0xFFFFFFFF;
        }
    }
}