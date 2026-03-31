using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.ARM;

namespace Trident.Core.CPU;

public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
{
    [TemplateParameter<bool>("UseImmediate", bit: 25)]
    [TemplateParameter<byte>("Operation", size: 4, hi: 24, lo: 21)]
    [TemplateParameter<bool>("SetFlags", bit: 20)]
    [TemplateParameter<bool>("ShiftByReg", bit: 4)]
    [TemplateGroup<ARMGroup>(ARMGroup.DataProcessing)]
    internal void ARM_DataProcessing<TTraits>(uint opcode)
        where TTraits : struct, IARM_DataProcessing_Traits
    {
        uint rd = (opcode >> 12) & 0xF;
        uint rn = (opcode >> 16) & 0xF;

        bool carry = Registers.IsFlagSet(Flags.C);

        Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
        
        uint op2;
        if (TTraits.UseImmediate)
        {
            int shamt = ((int)(opcode >> 8) & 0x0F) << 1;
            op2 = (opcode & 0xFF).RotateRight(shamt);
            if (shamt != 0) 
                carry = op2.IsBitSet(31);
        }
        else
        {
            byte shamt = TTraits.ShiftByReg
                ? (byte)Registers[(opcode >> 8) & 0xF]
                : (byte)((opcode >> 7) & 0x1F);

            if (TTraits.ShiftByReg)
            {
                Registers.PC += 4;
                Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;
            }

            ShiftType shiftType = (ShiftType)((opcode >> 5) & 0b11);
            op2 = PerformShift(
                shiftType, 
                immediateShift: !TTraits.ShiftByReg, 
                Registers[opcode & 0xF], 
                shamt, 
                ref carry
            );
        }

        uint op1 = Registers[rn];


        bool isTestOp = false;
        switch ((ALUOpARM)TTraits.Operation)
        {
            case ALUOpARM.AND: SetResultAndFlags(op1 &  op2); break;
            case ALUOpARM.EOR: SetResultAndFlags(op1 ^  op2); break;
            case ALUOpARM.ORR: SetResultAndFlags(op1 |  op2); break;
            case ALUOpARM.BIC: SetResultAndFlags(op1 & ~op2); break;
            case ALUOpARM.MVN: SetResultAndFlags(      ~op2); break;
            case ALUOpARM.MOV: SetResultAndFlags(       op2); break;

            case ALUOpARM.TST: SetNZ(op1 & op2); Registers.ModifyFlag(Flags.C, carry); isTestOp = true; break;
            case ALUOpARM.TEQ: SetNZ(op1 ^ op2); Registers.ModifyFlag(Flags.C, carry); isTestOp = true; break;
            case ALUOpARM.CMP: Subtract(op1, op2, true); isTestOp = true; break;
            case ALUOpARM.CMN: Add     (op1, op2, true); isTestOp = true; break;

            case ALUOpARM.ADD: Registers[rd] = Add          (op1, op2, TTraits.SetFlags); break;
            case ALUOpARM.SUB: Registers[rd] = Subtract     (op1, op2, TTraits.SetFlags); break;
            case ALUOpARM.RSB: Registers[rd] = Subtract     (op2, op1, TTraits.SetFlags); break;
            case ALUOpARM.ADC: Registers[rd] = AddCarry     (op1, op2, TTraits.SetFlags); break;
            case ALUOpARM.SBC: Registers[rd] = SubtractCarry(op1, op2, TTraits.SetFlags); break;
            case ALUOpARM.RSC: Registers[rd] = SubtractCarry(op2, op1, TTraits.SetFlags); break;
        }

        void SetResultAndFlags(uint result)
        {
            if (TTraits.SetFlags)
            {
                SetNZ(result);
                Registers.ModifyFlag(Flags.C, carry);
            }
            Registers[rd] = result;
        }


        bool shouldUpdatePC = TTraits.UseImmediate || !TTraits.ShiftByReg;
        bool isR15 = rd == 15;
        if (isR15)
        {
            if (TTraits.SetFlags && !RegisterSet.IsUserOrSystem(Registers.CurrentMode))
            {
                uint spsr = (uint)Registers.SPSR;
                Registers.SwitchMode((ProcessorMode)(spsr & 0x1F));
                Registers.CPSR = (Flags)spsr;
            }

            // Test operations never set Rd, so we shouldn't flush the pipeline even if it's encoded as R15.
            if (!isTestOp)
            {
                if (Registers.IsFlagSet(Flags.T)) ReloadPipelineThumb();
                else                              ReloadPipelineARM();
            }
        }

        if (shouldUpdatePC && (!isR15 || isTestOp))
            Registers.PC += 4;
    }
}