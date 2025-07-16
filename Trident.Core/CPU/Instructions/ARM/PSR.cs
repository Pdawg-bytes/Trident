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
        [TemplateParameter<bool>("UseImmediate", bit: 25)]
        [TemplateParameter<bool>("SPSR", bit: 22)]
        [TemplateParameter<bool>("ToStatusReg", bit: 21)]
        [TemplateGroup<ARMGroup>(ARMGroup.PSRTransfer)]
        internal void ARM_PSRTransfer<TTraits>(uint opcode)
            where TTraits : IARM_PSRTransfer_Traits
        {
            if (!TTraits.ToStatusReg)
            {
                uint rd = (opcode >> 12) & 0x0F;
                Registers[rd] = TTraits.SPSR ? (uint)Registers.SPSR : (uint)Registers.CPSR;
                goto Exit;
            }

            uint operand;
            uint statusMask = (opcode >> 16).BroadcastBits();

            if (TTraits.UseImmediate)
            {
                uint imm = opcode & 0xFF;
                int rotateAmount = (((int)opcode >> 8) & 0x0F) << 1;
                operand = imm.RotateRight(rotateAmount);
            }
            else
                operand = Registers[opcode & 0x0F];

            if (!TTraits.SPSR)
            {
                if (Registers.CurrentMode is PrivilegeMode.User)
                    statusMask &= 0xFF000000; // USR can only change conditions

                // Bit 4 (MSB of mode) is always forced to 1
                if ((statusMask & 0xFF) != 0)
                {
                    operand |= 1 << 4;
                    Registers.SwitchMode((PrivilegeMode)(operand & 0x1F));
                }

                Registers.CPSR = (Registers.CPSR & ~(Flags)statusMask) | (Flags)(operand & statusMask);
            }
            else
                Registers.SPSR = (Registers.SPSR & ~(Flags)statusMask) | (Flags)(operand & statusMask);

        Exit:
            Registers.PC += 4;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }
    }
}