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
        private enum ThumbALUOp : byte
        {
            AND = 0b0000,
            EOR = 0b0001,
            LSL = 0b0010,
            LSR = 0b0011,
            ASR = 0b0100,
            ADC = 0b0101,
            SBC = 0b0110,
            ROR = 0b0111,
            TST = 0b1000,
            NEG = 0b1001,
            CMP = 0b1010,
            CMN = 0b1011,
            ORR = 0b1100,
            MUL = 0b1101,
            BIC = 0b1110,
            MVN = 0b1111
        }


        [TemplateParameter<byte>("Operation", size: 4, hi: 9, lo: 6)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.ThumbALU)]
        internal void Thumb_DataProcessing<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_DataProcessing_Traits
        {
            uint rd = (uint)opcode & 0b111;
            uint rs = ((uint)opcode >> 3) & 0b111;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;

            switch ((ThumbALUOp)TTraits.Operation)
            {
                case ThumbALUOp.LSL:
                case ThumbALUOp.LSR:
                case ThumbALUOp.ASR:
                case ThumbALUOp.ROR:
                    {
                        uint shamt = Registers[rs];
                        Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;

                        bool carry = Registers.IsFlagSet(Flags.C);

                        ShiftType shiftType = (ShiftType)(
                            TTraits.Operation == 0b0111 
                                ? 0b11 
                                : TTraits.Operation - 0b0010);

                        Registers[rd] = PerformShift(shiftType, immediateShift: false, Registers[rd], (byte)shamt, ref carry);
                        SetNZ(Registers[rd]);
                        Registers.ModifyFlag(Flags.C, carry);
                        break;
                    }


                case ThumbALUOp.AND:
                case ThumbALUOp.EOR:
                case ThumbALUOp.ORR:
                case ThumbALUOp.BIC:
                case ThumbALUOp.MVN:
                    {
                        uint result = (ThumbALUOp)TTraits.Operation switch
                        {
                            ThumbALUOp.AND => Registers[rd] & Registers[rs],
                            ThumbALUOp.EOR => Registers[rd] ^ Registers[rs],
                            ThumbALUOp.ORR => Registers[rd] | Registers[rs],
                            ThumbALUOp.BIC => Registers[rd] & ~Registers[rs],
                            ThumbALUOp.MVN => ~Registers[rs],
                            _ => throw new InvalidInstructionException<TBus>($"Unexpected operation encoded in Thumb ALU: {TTraits.Operation}", this)
                        };
                        Registers[rd] = result;
                        SetNZ(result);
                        break;
                    }


                case ThumbALUOp.TST:
                case ThumbALUOp.CMP:
                case ThumbALUOp.CMN:
                    {
                        uint op1 = Registers[rd], op2 = Registers[rs];
                        switch ((ThumbALUOp)TTraits.Operation)
                        {
                            case ThumbALUOp.TST: SetNZ   (op1 & op2);      break;
                            case ThumbALUOp.CMP: Subtract(op1, op2, true); break;
                            case ThumbALUOp.CMN: Add     (op1, op2, true); break;
                        }
                        break;
                    }


                case ThumbALUOp.ADC:
                    Registers[rd] = AddCarry(Registers[rd], Registers[rs], true);
                    break;
                case ThumbALUOp.SBC:
                    Registers[rd] = SubtractCarry(Registers[rd], Registers[rs], true);
                    break;


                case ThumbALUOp.MUL:
                    {
                        Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;
                        Registers[rd] *= Registers[rs];
                        SetNZ(Registers[rd]);
                        // TODO: handle carry properly
                        // this is kind of optional; the C flag is unimportant (ARM7TDMI-manual part 2, page 24)
                        // however to achieve better accuracy, i should do it at some point
                        break;
                    }


                case ThumbALUOp.NEG:
                    Registers[rd] = Subtract(0, Registers[rs], true);
                    break;
            }
        }
    }
}