using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Bus;
using Trident.Core.Enums;

namespace Trident.Tests.SingleStep.Infrastructure
{
    internal struct TransactionalMemory : IDataBus
    {
        public ushort Read16(uint address, PipelineAccess access)
        {
            throw new NotImplementedException();
        }

        public uint Read32(uint address, PipelineAccess access)
        {
            throw new NotImplementedException();
        }

        public byte Read8(uint address, PipelineAccess access)
        {
            throw new NotImplementedException();
        }

        public void Write16(uint address, PipelineAccess access, ushort value)
        {
            throw new NotImplementedException();
        }

        public void Write32(uint address, PipelineAccess access, uint value)
        {
            throw new NotImplementedException();
        }

        public void Write8(uint address, PipelineAccess access, byte value)
        {
            throw new NotImplementedException();
        }
    }
}