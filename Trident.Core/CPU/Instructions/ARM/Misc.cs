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
            Registers.SetSPSRForMode(PrivilegeMode.UND, Registers.CPSR);
            Registers.SwitchMode(PrivilegeMode.UND);
            Registers.SetFlag(Flags.I);

            Registers.LR = Registers.PC - 4;
            Registers.PC = 0x00000004;
            ReloadPipelineARM();
        }


        [TemplateGroup<ARMGroup>(ARMGroup.SoftwareInterrupt)]
        internal void ARM_SoftwareInterrupt(uint opcode)
        {
            Registers.SetSPSRForMode(PrivilegeMode.SVC, Registers.CPSR);
            Registers.SwitchMode(PrivilegeMode.SVC);
            Registers.SetFlag(Flags.I);

            Registers.LR = Registers.PC - 4;
            Registers.PC = 0x00000008;
            ReloadPipelineARM();
        }
    }
}