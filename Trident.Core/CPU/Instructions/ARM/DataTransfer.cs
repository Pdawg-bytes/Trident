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
        [TemplateParameter<bool>("RegisterOffset", bit: 25)]
        [TemplateParameter<bool>("PreIndexed", bit: 24)]
        [TemplateParameter<bool>("AddOffset", bit: 23)]
        [TemplateParameter<bool>("ByteMode", bit: 22)]
        [TemplateParameter<bool>("Writeback", bit: 21)]
        [TemplateParameter<bool>("Load", bit: 20)]
        [TemplateGroup<ARMGroup>(ARMGroup.SingleDataTrasnfer)]
        internal void ARM_SingleDataTransfer<TTraits>(uint opcode)
            where TTraits : struct, IARM_SingleDataTransfer_Traits
        {
            uint rd = (opcode >> 12) & 0x0F;
            uint rn = (opcode >> 16) & 0x0F;
            uint address = Registers[rn];

            uint offset;
            if (TTraits.RegisterOffset)
            {
                bool carry = Registers.IsFlagSet(Flags.C);
                ShiftType shiftType = (ShiftType)((opcode >> 5) & 0b11);
                byte shamt = (byte)((opcode >> 7) & 0x1F);
                offset = PerformShift(shiftType, immediateShift: true, Registers[opcode & 0x0F], shamt, ref carry);
            }
            else
                offset = opcode & 0x0FFF;

            if (!TTraits.AddOffset)
                offset = (uint)-offset;

            Registers.PC += 4;
            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;


            address += TTraits.PreIndexed ? offset : 0;

            if (TTraits.Load)
            {
                uint value = TTraits.ByteMode
                    ? Bus.Read8(address, PipelineAccess.NonSequential)
                    : Read32Rotated(address, PipelineAccess.NonSequential);

                if (TTraits.Writeback || !TTraits.PreIndexed)
                {
                    Registers[rn] += offset;
                    if (rn == 15 && (rd != rn)) ReloadPipelineARM();
                }

                Registers[rd] = value;

                if (rd == 15) ReloadPipelineARM();
            }
            else
            {
                if (TTraits.ByteMode)
                    Bus.Write8(address, (byte)Registers[rd], PipelineAccess.NonSequential);
                else
                    Bus.Write32(address, Registers[rd], PipelineAccess.NonSequential);

                if (TTraits.Writeback || !TTraits.PreIndexed)
                {
                    Registers[rn] += offset;
                    if (rn == 15) ReloadPipelineARM();
                }
            }
        }


        [TemplateParameter<bool>("ByteMode", bit: 22)]
        [TemplateGroup<ARMGroup>(ARMGroup.Swap)]
        internal void ARM_Swap<TTraits>(uint opcode)
            where TTraits : struct, IARM_Swap_Traits
        {
            Registers.PC += 4;
            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;

            uint src = Registers[opcode & 0x0F];
            uint baseAddr = Registers[(opcode >> 16) & 0x0F];

            uint readRn;
            if (TTraits.ByteMode)
            {
                readRn = Bus.Read8(baseAddr, PipelineAccess.NonSequential);
                Bus.Write8(baseAddr, (byte)src, PipelineAccess.NonSequential | PipelineAccess.Lock);
            }
            else
            {
                readRn = Read32Rotated(baseAddr, PipelineAccess.NonSequential);
                Bus.Write32(baseAddr, src, PipelineAccess.NonSequential | PipelineAccess.Lock);
            }

            // TODO: wait state

            uint rd = (opcode >> 12) & 0x0F;
            Registers[rd] = readRn;
            if (rd == 15)
                ReloadPipelineARM();
        }
    }
}