using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.ARM;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateGroup<ARMGroup>(ARMGroup.BranchExchange)]
        internal void ARM_BranchExchange(uint opcode)
        {
            uint address = Registers[opcode & 0b1111];

            Registers.PC = address & 0xFFFFFFFE;

            if ((address & 1) != 0)
            {
                Registers.SetFlag(Flags.T);
                ReloadPipelineThumb();
            }
            else
                ReloadPipelineARM();
        }


        [TemplateParameter<bool>("Link", bit: 24)]
        [TemplateGroup<ARMGroup>(ARMGroup.BranchWithLink)]
        internal void ARM_BranchWithLink<TTraits>(uint opcode)
            where TTraits : IARM_BranchWithLink_Traits
        {
            int offset = ((opcode & 0xFFFFFF).ExtendFrom(24)) << 2;

            if (TTraits.Link) Registers.LR = Registers.PC - 4;
            Registers.PC += (uint)offset;

            ReloadPipelineARM();
        }
    }
}