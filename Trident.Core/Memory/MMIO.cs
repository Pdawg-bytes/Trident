using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;

namespace Trident.Core.Memory
{
    internal class MMIO
    {
        private readonly MemoryAccessHandler _handler;

        internal MMIO()
        {
            _handler = new MemoryAccessHandler
            (
                read8: this.Read8,
                read16: this.Read16,
                read32: this.Read32,

                write8: this.Write8,
                write16: this.Write16,
                write32: this.Write32,

                dispose: null
            );
        }

        internal void AttachToBus(ref GBABus bus) => bus.RegisterHandler(0x04, _handler);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte Read8(uint address, PipelineAccess access) => 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort Read16(uint address, PipelineAccess access) => 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read32(uint address, PipelineAccess access) => 0;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write8(uint address, PipelineAccess access, byte value) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write16(uint address, PipelineAccess access, ushort value) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write32(uint address, PipelineAccess access, uint value) { }
    }
}