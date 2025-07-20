using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.Thumb;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateParameter<byte>("Operation", size: 2, hi: 12, lo: 11)]
        [TemplateParameter<byte>("ShiftAmt", size: 5, hi: 10, lo: 6)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.ShiftImmediate)]
        internal void Thumb_ShiftImmediate<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_ShiftImmediate_Traits
        {
            uint operand = Registers[(opcode >> 3) & 0b111];
            uint rd = (uint)opcode & 0b111;

            if (TTraits.Operation > 2)
                throw new InvalidInstructionException<TBus>($"Shift immediate: unexpected sub-operation {TTraits.Operation}", this);

            bool carry = Registers.IsFlagSet(Flags.C);
            uint result = Registers[rd] = PerformShift((ShiftType)TTraits.Operation, immediateShift: true, operand, TTraits.ShiftAmt, ref carry);

            Registers.ModifyFlag(Flags.C, carry);
            SetNZ(result);

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }
    }
}