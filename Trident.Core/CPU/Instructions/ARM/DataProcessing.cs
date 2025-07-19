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
        [TemplateParameter<bool>("ImmediateOperand", bit: 25)]
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
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
            
            uint op2;

            if (TTraits.ImmediateOperand)
            {
                int shamt = ((int)(opcode >> 8) & 0x0F) << 1;
                op2 = (opcode & 0xFF).RotateRight(shamt);
                if (shamt != 0) 
                    carry = (op2 >> 31) != 0;
            }
            else
            {
                byte shamt = TTraits.ShiftByReg
                    ? (byte)Registers[(opcode >> 8) & 0xF]
                    : (byte)((opcode >> 7) & 0x1F);

                if (TTraits.ShiftByReg)
                {
                    Registers.PC += 4;
                    Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;
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

            uint result = 0;
            switch ((ALUOpARM)TTraits.Operation)
            {
                case ALUOpARM.AND: result = op1 &  op2; break;
                case ALUOpARM.EOR: result = op1 ^  op2; break;
                case ALUOpARM.ORR: result = op1 |  op2; break;
                case ALUOpARM.BIC: result = op1 & ~op2; break;
                case ALUOpARM.MVN: result =       ~op2; break;
                case ALUOpARM.MOV: result =        op2; break;

                case ALUOpARM.TST: SetNZ(op1 & op2); Registers.ModifyFlag(Flags.C, carry); goto UpdatePC;
                case ALUOpARM.TEQ: SetNZ(op1 ^ op2); Registers.ModifyFlag(Flags.C, carry); goto UpdatePC;
                case ALUOpARM.CMP: Subtract(op1, op2, true); goto UpdatePC;
                case ALUOpARM.CMN: Add(op1, op2, true); goto UpdatePC;

                case ALUOpARM.ADD: Registers[rd] = Add          (op1, op2, TTraits.SetFlags); goto UpdatePC;
                case ALUOpARM.SUB: Registers[rd] = Subtract     (op1, op2, TTraits.SetFlags); goto UpdatePC;
                case ALUOpARM.RSB: Registers[rd] = Subtract     (op2, op1, TTraits.SetFlags); goto UpdatePC;
                case ALUOpARM.ADC: Registers[rd] = AddCarry     (op1, op2, TTraits.SetFlags); goto UpdatePC;
                case ALUOpARM.SBC: Registers[rd] = SubtractCarry(op1, op2, TTraits.SetFlags); goto UpdatePC;
                case ALUOpARM.RSC: Registers[rd] = SubtractCarry(op2, op1, TTraits.SetFlags); goto UpdatePC;
            }

            if (TTraits.SetFlags)
            {
                SetNZ(result);
                Registers.ModifyFlag(Flags.C, carry);
            }
            Registers[rd] = result;


        UpdatePC:
            bool shouldUpdatePC = TTraits.ImmediateOperand || !TTraits.ShiftByReg;
            if (rd == 15)
            {
                if (TTraits.SetFlags)
                {
                    uint spsr = (uint)Registers.SPSR;
                    Registers.SwitchMode((PrivilegeMode)(spsr & 0x1F));
                    Registers.CPSR = (Flags)spsr;
                }

                // Test operations never set Rd, so we shouldn't flush the pipeline even if it's encoded as R15.
                bool isTestOp = TTraits.Operation is (byte)ALUOpARM.TST or (byte)ALUOpARM.TEQ or (byte)ALUOpARM.CMP or (byte)ALUOpARM.CMN;
                if (!isTestOp)
                {
                    if (Registers.IsFlagSet(Flags.T)) ReloadPipelineThumb();
                    else ReloadPipelineARM();
                }
                else if (shouldUpdatePC)
                    Registers.PC += 4;
            }
            else if (shouldUpdatePC)
                Registers.PC += 4;
        }
    }
}