using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.Thumb;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateParameter<bool>("HighRegister", bit: 6)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.BranchExchange)]
        internal void Thumb_BranchExchange<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_BranchExchange_Traits
        {
            uint rs = ((uint)opcode >> 3) & 0b111;
            if (TTraits.HighRegister) rs |= 8;

            uint address = Registers[rs];

            Registers.PC = address & 0xFFFFFFFE;
            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;

            if ((address & 1) == 0)
            {
                Registers.ClearFlag(Flags.T);
                ReloadPipelineARM();
            }
            else
                ReloadPipelineThumb();
        }


        [TemplateParameter<byte>("Condition", size: 4, hi: 11, lo: 8)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.ConditionalBranch)]
        internal void Thumb_ConditionalBranch<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_ConditionalBranch_Traits
        {
            if (Conditions.ConditionMet(TTraits.Condition, Registers.CPSR))
            {
                Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;
                Registers.PC += (uint)((uint)opcode & 0xFF).ExtendFrom(8) << 1;
                ReloadPipelineThumb();
            }
            else
            {
                Registers.PC += 2;
                Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
            }
        }


        [TemplateGroup<ThumbGroup>(ThumbGroup.UnconditionalBranch)]
        internal void Thumb_UnconditionalBranch(ushort opcode)
        {
            Registers.PC += (uint)((uint)opcode & 0x07FF).ExtendFrom(11) << 1;
            ReloadPipelineThumb();
        }


        [TemplateParameter<bool>("CompletesBranch", bit: 11)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LongBranchWithLink)]
        internal void Thumb_LongBranchWithLink<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_LongBranchWithLink_Traits
        {
            uint offset = (uint)opcode & 0x07FF;

            if (!TTraits.CompletesBranch)
                Thumb_LongBranchPrefix(offset);
            else
                Thumb_LongBranchSuffix(offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Thumb_LongBranchPrefix(uint offset)
        {
            Registers.PC += 2;
            Registers.LR = Registers.PC + ((uint)offset.ExtendFrom(11) << 12) - 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Thumb_LongBranchSuffix(uint offset)
        {
            uint returnAddress = (Registers.PC - 2) | 1; // Indicate Thumb state
            Registers.PC = (Registers.LR + (offset << 1)) & 0xFFFFFFFE;
            Registers.LR = returnAddress;
            ReloadPipelineThumb();
        }
    }
}