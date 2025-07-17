using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.ARM;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateParameter<bool>("ByteMode", bit: 22)]
        [TemplateGroup<ARMGroup>(ARMGroup.Swap)]
        internal void ARM_Swap<TTraits>(uint opcode)
            where TTraits : struct, IARM_Swap_Traits
        {
            Registers.PC += 4;
            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;

            uint src = Registers[opcode & 0x0F];
            uint addr = Registers[(opcode >> 16) & 0x0F];

            uint readRn = 0;
            if (TTraits.ByteMode)
            {
                readRn = Bus.Read8(addr, PipelineAccess.NonSequential);
                Bus.Write8(addr, (byte)src, PipelineAccess.NonSequential | PipelineAccess.Lock);
            }
            else
            {
                readRn = Read32Rotated(addr, PipelineAccess.NonSequential);
                Bus.Write32(addr, src, PipelineAccess.NonSequential | PipelineAccess.Lock);
            }

            // TODO: wait state

            uint rd = (opcode >> 12) & 0x0F;
            Registers[rd] = readRn;
            if (rd == 15)
                ReloadPipelineARM();
        }
    }
}