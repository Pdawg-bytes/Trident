using Trident.Core.Global;

namespace Trident.Core.Memory.MappedIO
{
    internal partial class MMIO
    {
        private void InitializeRegisterMap()
        {

        }


        internal MemoryAccessHandler GetAccessHandler() => new
        (
            read8: (address, _) => { _step(1); return Read8(address); },
            read16: (address, _) => { _step(1); return Read16(address.Align<ushort>()); },
            read32: (address, _) => { _step(1); return Read32(address.Align<uint>()); },

            write8: (address, _, value) => { _step(1); Write8(address, value); },
            write16: (address, _, value) => { _step(1); Write16(address.Align<ushort>(), value); },
            write32: (address, _, value) => { _step(1); Write32(address.Align<uint>(), value); },

            dispose: null
)       ;
    }
}