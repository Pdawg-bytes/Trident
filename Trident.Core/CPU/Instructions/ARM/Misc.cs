using Trident.Core.Bus;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateGroup<ARMGroup>(ARMGroup.Undefined)]
        internal void ARM_Undefined(uint opcode)
        {
            Registers.SetSPSRForMode(PrivilegeMode.Undefined, Registers.CPSR);
            Registers.SwitchMode(PrivilegeMode.Undefined);
            Registers.SetFlag(Flags.I);

            Registers.LR = Registers.PC - 4;
            Registers.PC = 0x00000004;
            ReloadPipelineARM();
        }


        [TemplateGroup<ARMGroup>(ARMGroup.SoftwareInterrupt)]
        internal void ARM_SoftwareInterrupt(uint opcode)
        {
            Registers.SetSPSRForMode(PrivilegeMode.Supervisor, Registers.CPSR);
            Registers.SwitchMode(PrivilegeMode.Supervisor);
            Registers.SetFlag(Flags.I);

            Registers.LR = Registers.PC - 4;
            Registers.PC = 0x00000008;
            ReloadPipelineARM();
        }
    }
}