using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Bus;
using Trident.Core.CPU;
using Trident.Core.Memory;

namespace Trident.Core.Machine
{
    public class GBA
    {
        internal ARM7TDMI CPU;
        internal DataBus Bus;

        private readonly BIOS _bios;
        private readonly UnusedSection _unused = new();
        private readonly EWRAM _eWRAM = new();
        private readonly IWRAM _iWRAM = new();

        public GBA()
        {
            CPU = new();
            Bus = new DataBus();

            _bios = new(() => CPU.Registers.GetRegisterRef(15));

            CPU.AttachBus(Bus);
        }

        public void AttachBIOS(byte[] bios) => _bios.LoadBIOS(bios);

        public void AttachGamePak(byte[] rom)
        {

        }
    }
}