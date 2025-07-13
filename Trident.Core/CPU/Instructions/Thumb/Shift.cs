using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Registers;
using Trident.Core.CPU.Decoding.Thumb;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        internal void Thumb_ShiftImmediate(ref ThumbArguments args)
        {
            uint src = Registers[args.Rs];

            if (args.SubOp > 2)
                throw new InvalidInstructionException<TBus>($"Shift immediate: unexpected sub-operation {args.SubOp}", this);

            bool carry = Registers.IsFlagSet(Flags.C);
            uint result = Registers[args.Rd] = PerformShift((ShiftType)args.SubOp, immediateShift: true, src, args.Imm, ref carry);

            Registers.ModifyFlag(Flags.C, carry);
            SetZeroSign(result);

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }
    }
}