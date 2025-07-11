using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Registers;
using Trident.Core.CPU.Decoding.Thumb;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        internal void Thumb_BranchExchange(ref ThumbArguments args)
        {
            uint address = Registers[args.Rs];

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

        internal void Thumb_ConditionalBranch(ref ThumbArguments args)
        {
            Registers.PC += 2;
            if (Conditions.ConditionMet(args.SubOp, Registers.CPSR))
            {
                Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;
                Registers.PC += args.Imm - 2;
                ReloadPipelineThumb();
            }
            else
                Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }

        internal void Thumb_Branch(ref ThumbArguments args)
        {
            Registers.PC += args.Imm;
            ReloadPipelineThumb();
        }

        internal void Thumb_LongBranchPrefix(ref ThumbArguments args)
        {
            Registers.PC += 2;
            Registers.LR = Registers.PC + args.Imm - 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }

        internal void Thumb_LongBranchSuffix(ref ThumbArguments args)
        {
            uint returnAddress = (Registers.PC - 2) | 1; // Indicate Thumb state
            Registers.PC = (Registers.LR + args.Imm) & 0xFFFFFFFE;
            Registers.LR = returnAddress;
            ReloadPipelineThumb();
        }
    }
}