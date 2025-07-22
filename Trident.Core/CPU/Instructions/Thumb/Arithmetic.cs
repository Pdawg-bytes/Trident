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
        [TemplateParameter<bool>("UseImmediate", bit: 10)]
        [TemplateParameter<bool>("Subtract", bit: 9)]
        [TemplateParameter<byte>("Param3", size: 3, hi: 8, lo: 6)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.AddSubtract)]
        internal void Thumb_AddSub<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_AddSub_Traits
        {
            uint operand = TTraits.UseImmediate ? TTraits.Param3 : Registers[TTraits.Param3];

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;

            uint rd = (uint)opcode & 0b111;
            uint src = Registers[(opcode >> 3) & 0b111];

            if (TTraits.Subtract) 
                Registers[rd] = Subtract(src, operand, modifyFlags: true);
            else 
                Registers[rd] = Add(src, operand, modifyFlags: true);
        }


        [TemplateParameter<bool>("Subtract", bit: 7)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.AddOffsetSP)]
        internal void Thumb_AddOffsetSP<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_AddOffsetSP_Traits
        {
            uint immOffset = ((uint)opcode & 0x7F) << 2;
            Registers.SP += TTraits.Subtract ? (uint)-immOffset : immOffset;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
        }


        [TemplateParameter<byte>("Operation", size: 2, hi: 12, lo: 11)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.UnnamedGroup3)]
        internal void Thumb_MovCmpAddSubImm<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_MovCmpAddSubImm_Traits
        {
            uint rd = ((uint)opcode >> 8) & 0b111;
            uint immOperand = (uint)opcode & 0xFF;

            switch (TTraits.Operation)
            {
                case 0b00: // MOV
                    Registers[rd] = immOperand;
                    Registers.ClearFlag(Flags.N);
                    Registers.ModifyFlag(Flags.Z, immOperand == 0);
                    break;
                case 0b01: // CMP
                    Subtract(Registers[rd], immOperand, modifyFlags: true);
                    break;
                case 0b10: // ADD
                    Registers[rd] = Add(Registers[rd], immOperand, modifyFlags: true);
                    break;
                case 0b11: // SUB
                    Registers[rd] = Subtract(Registers[rd], immOperand, modifyFlags: true);
                    break;
            }

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
        }


        [TemplateParameter<byte>("Operation", size: 2, hi: 9, lo: 8)]
        [TemplateParameter<bool>("High1", bit: 7)]
        [TemplateParameter<bool>("High2", bit: 6)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.HiRegisterOps)]
        internal void Thumb_HighRegister<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_HighRegister_Traits
        {
            uint rs = (((uint)opcode >> 3) & 0b111) | (uint)(TTraits.High2 ? 8 : 0);
            uint rd = ((uint)opcode & 0b111)        | (uint)(TTraits.High1 ? 8 : 0);

            uint op1 = Registers[rs];
            uint op2 = Registers[rd];

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;

            switch (TTraits.Operation)
            {
                case 0b00: // ADD
                    Registers[rd] = op1 + op2;
                    break;
                case 0b01: // CMP
                    Subtract(op2, op1, modifyFlags: true);
                    return;
                case 0b10: // MOV
                    Registers[rd] = op1;
                    break;
                case 0b11:
                    throw new InvalidInstructionException<TBus>("BX encoded in high-register operation.", this);
            }

            if (rd == 15)
            {
                Registers.PC &= 0xFFFFFFFE;
                ReloadPipelineThumb();
            }
        }
    }
}