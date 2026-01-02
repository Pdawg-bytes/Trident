using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.ARM;

namespace Trident.Core.CPU;

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

        address += TTraits.PreIndexed ? offset : 0;

        Registers.PC += 4;
        Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;


        if (TTraits.Load)
        {
            uint value = TTraits.ByteMode
                ? Bus.Read8(address, PipelineAccess.NonSequential)
                : Read32Rotated(address, PipelineAccess.NonSequential);

            if (TTraits.Writeback || !TTraits.PreIndexed)
            {
                Registers[rn] += offset;
                if (rn == 15 && (rn != rd)) ReloadPipelineARM();
            }

            // TODO: wait state

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


    [TemplateParameter<bool>("PreIndexed", bit: 24)]
    [TemplateParameter<bool>("AddOffset", bit: 23)]
    [TemplateParameter<bool>("UseImmediate", bit: 22)]
    [TemplateParameter<bool>("Writeback", bit: 21)]
    [TemplateParameter<bool>("Load", bit: 20)]
    [TemplateParameter<byte>("Operation", size: 2, hi: 6, lo: 5)]
    [TemplateGroup<ARMGroup>(ARMGroup.SmallSignedTransfer)]
    internal void ARM_SignedDataTransfer<TTraits>(uint opcode)
        where TTraits : struct, IARM_SignedDataTransfer_Traits
    {
        uint rd = (opcode >> 12) & 0x0F;
        uint rn = (opcode >> 16) & 0x0F;
        uint address = Registers[rn];

        uint offset = TTraits.UseImmediate ?
            ((opcode >> 4) & 0xF0) | (opcode & 0x0F) :
            Registers[opcode & 0x0F];

        if (!TTraits.AddOffset)
            offset = (uint)-offset;

        address += TTraits.PreIndexed ? offset : 0;

        Registers.PC += 4;
        Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;


        if (TTraits.Operation == 0b00)
            throw new InvalidInstructionException<TBus>("0b00 encoded as operation in signed data transfer; should be handled by SWP.", this);

        uint value = 0;
        if (TTraits.Load)
        {
            value = TTraits.Operation switch
            {
                0b01 => Read16Rotated(address, PipelineAccess.NonSequential),
                0b10 => Read8Extended(address, PipelineAccess.NonSequential),
                0b11 => Read16Extended(address, PipelineAccess.NonSequential),
                _    => throw new InvalidInstructionException<TBus>($"Invalid operation encoded in ARM Signed Data Transfer: {TTraits.Operation}", this)
            };
            // TODO: wait state
        }
        else if (TTraits.Operation == 0b01)
            Bus.Write16(address, (ushort)Registers[rd], PipelineAccess.NonSequential);


        if (TTraits.Writeback || !TTraits.PreIndexed)
        {
            Registers[rn] += offset;

            bool shouldReload = rn == 15;
            if (TTraits.Load)
                shouldReload &= rn != rd;

            if (shouldReload)
                ReloadPipelineARM();
        }

        if (TTraits.Load)
        {
            Registers[rd] = value;
            if (rd == 15)
                ReloadPipelineARM();
        }
    }


    [TemplateParameter<bool>("ByteMode", bit: 22)]
    [TemplateGroup<ARMGroup>(ARMGroup.Swap)]
    internal void ARM_Swap<TTraits>(uint opcode)
        where TTraits : struct, IARM_Swap_Traits
    {
        Registers.PC += 4;
        Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

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