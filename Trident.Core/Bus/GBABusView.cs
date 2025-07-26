using Trident.Core.CPU.Pipeline;

namespace Trident.Core.Bus
{
    public class GBABusView : IDisposable
    {
        private Func<uint, PipelineAccess, byte>? _read8;
        private Func<uint, PipelineAccess, ushort>? _read16;
        private Func<uint, PipelineAccess, uint>? _read32;

        private Action<uint, byte, PipelineAccess>? _write8;
        private Action<uint, ushort, PipelineAccess>? _write16;
        private Action<uint, uint, PipelineAccess>? _write32;


        public GBABusView(ref GBABus bus)
        {
            _read8 = bus.Read8;
            _read16 = bus.Read16;
            _read32 = bus.Read32;
            _write8 = bus.Write8;
            _write16 = bus.Write16;
            _write32 = bus.Write32;
        }

        public byte Read8(uint address, PipelineAccess access) => _read8!(address, access);
        public ushort Read16(uint address, PipelineAccess access) => _read16!(address, access);
        public uint Read32(uint address, PipelineAccess access) => _read32!(address, access);

        public void Write8(uint address, byte value, PipelineAccess access) => _write8!(address, value, access);
        public void Write16(uint address, ushort value, PipelineAccess access) => _write16!(address, value, access);
        public void Write32(uint address, uint value, PipelineAccess access) => _write32!(address, value, access);


        public void Dispose()
        {
            _read8 = null;
            _read16 = null;
            _read32 = null;

            _write8 = null;
            _write16 = null;
            _write32 = null;
        }
    }
}