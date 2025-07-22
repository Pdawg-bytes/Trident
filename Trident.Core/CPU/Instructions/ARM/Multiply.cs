using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.ARM;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateParameter<bool>("Accumulate", bit: 21)]
        [TemplateParameter<bool>("SetFlags", bit: 20)]
        [TemplateGroup<ARMGroup>(ARMGroup.Multiply)]
        internal void ARM_Multiply<TTraits>(uint opcode)
            where TTraits : IARM_Multiply_Traits
        {
            uint rd = (opcode >> 16) & 0x0F;

            Registers.PC += 4;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

            uint multiplier   = Registers[opcode & 0x0F];
            uint multiplicand = Registers[(opcode >> 8) & 0x0F];

            uint mulResult = multiplier * multiplicand;

            if (TTraits.Accumulate)
            {
                mulResult += Registers[(opcode >> 12) & 0x0F];
                // TODO: wait state
            }

            // TODO: handle carry properly
            // this is kind of optional; the C flag is unimportant (ARM7TDMI-manual part 2, page 24)
            // however to achieve better accuracy, i should do it at some point
            if (TTraits.SetFlags)
                SetNZ(mulResult);

            Registers[rd] = mulResult;

            if (rd == 15)
                ReloadPipelineARM();
        }


        [TemplateParameter<bool>("Signed", bit: 22)]
        [TemplateParameter<bool>("Accumulate", bit: 21)]
        [TemplateParameter<bool>("SetFlags", bit: 20)]
        [TemplateGroup<ARMGroup>(ARMGroup.MultiplyLong)]
        internal void ARM_MultiplyLong<TTraits>(uint opcode)
            where TTraits : struct, IARM_MultiplyLong_Traits
        {
            uint rdHi = (opcode >> 16) & 0x0F;
            uint rdLo = (opcode >> 12) & 0x0F;

            Registers.PC += 4;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

            uint multiplier   = Registers[opcode & 0x0F];
            uint multiplicand = Registers[(opcode >> 8) & 0x0F];

            ulong mulResult = TTraits.Signed ?
                (ulong)((long)(int)multiplier * (long)(int)multiplicand) :
                (ulong)multiplier * (ulong)multiplicand;

            // TODO: wait state

            if (TTraits.Accumulate)
            {
                mulResult += ((ulong)Registers[rdHi] << 32) | Registers[rdLo];
                // TODO: wait state
            }

            uint resultHi = (uint)(mulResult >> 32);

            if (TTraits.SetFlags)
            {
                Registers.ModifyFlag(Flags.N, resultHi.IsBitSet(31));
                Registers.ModifyFlag(Flags.Z, mulResult == 0);
                // TODO: handle carry properly
                // this is *also* kind of optional; the C flag is unimportant (ARM7TDMI-manual part 2, page 26)
                // however to achieve better accuracy, i should do it at some point
            }

            Registers[rdLo] = (uint)(mulResult & uint.MaxValue);
            Registers[rdHi] = resultHi;

            if (rdHi == 15 || rdLo == 15)
                ReloadPipelineARM();
        }
    }
}