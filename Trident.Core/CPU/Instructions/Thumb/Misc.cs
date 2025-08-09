using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.Thumb;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateGroup<ThumbGroup>(ThumbGroup.SoftwareInterrupt)]
        internal void Thumb_SWI(ushort opcode)
        {
            Registers.SetSPSRForMode(PrivilegeMode.SVC, Registers.CPSR);
            Registers.SwitchMode(PrivilegeMode.SVC);
            Registers.LR = Registers.PC - 2; // LR is now R14_svc because of mode switch.

            Registers.ClearFlag(Flags.T);
            Registers.SetFlag(Flags.I);

            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;
            Registers.PC = 0x00000008;
            ReloadPipelineARM();
        }


        [TemplateParameter<bool>("UseSP", bit: 11)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadAddress)]
        internal void Thumb_LoadAddress<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_LoadAddress_Traits
        {
            uint rd = ((uint)opcode >> 8) & 0b111;
            uint immOffset = ((uint)opcode & 0xFF) << 2;

            // When loading from PC, the address must be word-aligned.
            Registers[rd] = TTraits.UseSP ?
                Registers.SP + immOffset  :
                Registers.PC.Align<uint>() + immOffset;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
        }
    }
}