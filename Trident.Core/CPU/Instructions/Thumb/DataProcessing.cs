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
        [TemplateParameter<byte>("Operation", size: 4, hi: 9, lo: 6)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.ThumbALU)]
        internal void Thumb_DataProcessing<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_DataProcessing_Traits
        {
            uint rd = (uint)opcode & 0b111;
            uint rs = ((uint)opcode >> 3) & 0b111;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;

            switch ((ALUOpThumb)TTraits.Operation)
            {
                case ALUOpThumb.LSL:
                case ALUOpThumb.LSR:
                case ALUOpThumb.ASR:
                case ALUOpThumb.ROR:
                    {
                        uint shamt = Registers[rs];
                        Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

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


                case ALUOpThumb.AND:
                case ALUOpThumb.EOR:
                case ALUOpThumb.ORR:
                case ALUOpThumb.BIC:
                case ALUOpThumb.MVN:
                    {
                        uint result = (ALUOpThumb)TTraits.Operation switch
                        {
                            ALUOpThumb.AND => Registers[rd]  &  Registers[rs],
                            ALUOpThumb.EOR => Registers[rd]  ^  Registers[rs],
                            ALUOpThumb.ORR => Registers[rd]  |  Registers[rs],
                            ALUOpThumb.BIC => Registers[rd]  & ~Registers[rs],
                            ALUOpThumb.MVN => ~Registers[rs],
                            _ => throw new InvalidInstructionException<TBus>($"Unexpected operation encoded in Thumb ALU: {TTraits.Operation}", this)
                        };
                        Registers[rd] = result;
                        SetNZ(result);
                        break;
                    }


                case ALUOpThumb.TST:
                case ALUOpThumb.CMP:
                case ALUOpThumb.CMN:
                    {
                        uint op1 = Registers[rd], op2 = Registers[rs];
                        switch ((ALUOpThumb)TTraits.Operation)
                        {
                            case ALUOpThumb.TST: SetNZ   (op1 & op2);      break;
                            case ALUOpThumb.CMP: Subtract(op1, op2, true); break;
                            case ALUOpThumb.CMN: Add     (op1, op2, true); break;
                        }
                        break;
                    }


                case ALUOpThumb.ADC:
                    Registers[rd] = AddCarry(Registers[rd], Registers[rs], true);
                    break;
                case ALUOpThumb.SBC:
                    Registers[rd] = SubtractCarry(Registers[rd], Registers[rs], true);
                    break;


                case ALUOpThumb.MUL:
                    {
                        Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;
                        Registers[rd] *= Registers[rs];
                        SetNZ(Registers[rd]);
                        // TODO: handle carry properly
                        // this is kind of optional; the C flag is unimportant (ARM7TDMI-manual part 2, page 24)
                        // however to achieve better accuracy, i should do it at some point
                        break;
                    }


                case ALUOpThumb.NEG:
                    Registers[rd] = Subtract(0, Registers[rs], true);
                    break;
            }
        }
    }
}