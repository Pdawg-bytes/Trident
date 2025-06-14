using Trident.Core.Enums;

namespace Trident.Core.Bus
{
    public interface IDataBus
    {
        public byte Read8(uint address, PipelineAccess access);
        public ushort Read16(uint address, PipelineAccess access);
        public uint Read32(uint address, PipelineAccess access);

        public void Write8(uint address, PipelineAccess access, byte value);
        public void Write16(uint address, PipelineAccess access, ushort value);
        public void Write32(uint address, PipelineAccess access, uint value);
    }
}