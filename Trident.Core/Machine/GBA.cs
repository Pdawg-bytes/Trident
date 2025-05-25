using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Bus;
using Trident.Core.CPU;

namespace Trident.Core.Machine
{
    public class GBA
    {
        internal ARM7TDMI CPU;
        internal DataBus Bus;

        public GBA()
        {
            CPU = new();

            // Initialize bus now that the components are initialized.
            Bus = new DataBus(CPU);

            // Attach the bus to everything.
            CPU.AttachBus(Bus);
        }
    }
}