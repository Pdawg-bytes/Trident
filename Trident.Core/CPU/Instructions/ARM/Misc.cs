using Trident.Core.Bus;
using Trident.Core.CPU.Registers;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        internal void ARM_UND(uint opcode)
        {
            Registers.SetSPSR(PrivilegeMode.Undefined, Registers.CPSR);
            Registers.SwitchMode(PrivilegeMode.Undefined);
            Registers.SetFlag(Flags.I);

            Registers.LR = Registers.PC - 4;
            Registers.PC = 0x00000004;
            ReloadPipelineARM();
        }

        internal void ARM_SWI(uint opcode)
        {
            Registers.SetSPSR(PrivilegeMode.Supervisor, Registers.CPSR);
            Registers.SwitchMode(PrivilegeMode.Supervisor);
            Registers.SetFlag(Flags.I);

            Registers.LR = Registers.PC - 4;
            Registers.PC = 0x00000008;
            ReloadPipelineARM();
        }
    }
}