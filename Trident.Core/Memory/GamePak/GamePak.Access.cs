using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Enums;

namespace Trident.Core.Memory.GamePak
{
    internal partial class GamePak
    {
        private MemoryAccessHandler GetHandler<TAccess>() where TAccess : struct, IAccess
        {
            return new MemoryAccessHandler()
            {
                Read8 = Read8<TAccess>,
                Read16 = Read16<TAccess>,
                Read32 = Read32<TAccess>,

                Dispose = Dispose
            };
        }

        private byte Read8<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
        {
            ushort shift = (ushort)((address & 1) << 3);
            return (byte)(ReadData16<TAccess>(address, access) >> shift);
        }
        private ushort Read16<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
        {
            return ReadData16<TAccess>(address, access);
        }
        private uint Read32<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
        {
            return ReadData32<TAccess>(address, access);
        }
    }
}