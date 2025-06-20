using Trident.Core.CPU.Pipeline;

namespace Trident.Core.Bus
{
    public interface IDataBus
    {
        public byte Read8(uint address, PipelineAccess access);
        public ushort Read16(uint address, PipelineAccess access);
        public uint Read32(uint address, PipelineAccess access);

        public void Write8(uint address, byte value, PipelineAccess access);
        public void Write16(uint address, ushort value, PipelineAccess access);
        public void Write32(uint address, uint value, PipelineAccess access);
    }
}