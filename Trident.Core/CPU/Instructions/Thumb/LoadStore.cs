using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.Thumb;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadPCRelative)]
        internal void Thumb_LoadPCRelative(ushort opcode)
        {
            uint offset = (uint)opcode & 0xFF;
            uint target = Registers.PC.Align<uint>() + (offset << 2);

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;

            Registers[(opcode >> 8) & 0b111] = Bus.Read32(target, PipelineAccess.NonSequential);
            // TODO: wait state
        }


        [TemplateParameter<bool>("Load", bit: 11)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadStoreSPRelative)]
        internal void Thumb_LoadStoreSPRelative<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_LoadStoreSPRelative_Traits
        {
            uint rd = ((uint)opcode >> 8) & 0b111;

            uint offset = (uint)opcode & 0xFF;
            uint target = Registers.SP + (offset << 2);

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;

            if (TTraits.Load)
            {
                Registers[rd] = Read32Rotated(target, PipelineAccess.NonSequential);
                // TODO: wait state
            }
            else
                Bus.Write32(target, Registers[rd], PipelineAccess.NonSequential);
        }


        [TemplateParameter<byte>("Operation", size: 2, hi: 11, lo: 10)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadStoreRegOffset)]
        internal void Thumb_LoadStoreRegOffset<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_LoadStoreRegOffset_Traits
        {
            uint rd = (uint)opcode & 0b111;

            uint @base  = Registers[(opcode >> 3) & 0b111];
            uint offset = Registers[(opcode >> 6) & 0b111];
            uint target = @base + offset;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;

            switch (TTraits.Operation & 0b11)
            {
                case 0b00: // STR
                    Bus.Write32(target, Registers[rd], PipelineAccess.NonSequential);
                    break;
                case 0b01: // STRB
                    Bus.Write8(target, (byte)Registers[rd], PipelineAccess.NonSequential);
                    break;
                case 0b10: // LDR
                    Registers[rd] = Read32Rotated(target, PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
                case 0b11: // LDRB
                    Registers[rd] = Bus.Read8(target, PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
            }
        }


        [TemplateParameter<byte>("Operation", size: 2, hi: 12, lo: 11)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadStoreImmOffset)]
        internal void Thumb_LoadStoreImmOffset<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_LoadStoreImmOffset_Traits
        {
            uint rd = (uint)opcode & 0b111;

            uint baseAddr = Registers[(opcode >> 3) & 0b111];
            uint imm = (uint)(opcode >> 6) & 0x1F;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;

            switch (TTraits.Operation & 0b11)
            {
                case 0b00: // STR
                    Bus.Write32(baseAddr + (imm << 2), Registers[rd], PipelineAccess.NonSequential);
                    break;
                case 0b01: // LDR
                    Registers[rd] = Read32Rotated(baseAddr + (imm << 2), PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
                case 0b10: // STRB
                    Bus.Write8(baseAddr + imm, (byte)Registers[rd], PipelineAccess.NonSequential);
                    break;
                case 0b11: // LDRB
                    Registers[rd] = Bus.Read8(baseAddr + imm, PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
            }
        }


        [TemplateParameter<bool>("Load", bit: 11)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadStore16)]
        internal void Thumb_LoadStore16ImmOffset<TTraits>(ushort opcode)
            where TTraits : IThumb_LoadStore16ImmOffset_Traits
        {
            uint rd = (uint)opcode & 0b111;

            uint baseAddr = Registers[(opcode >> 3) & 0b111];
            uint imm = ((uint)opcode >> 6) & 0x1F;
            uint target = baseAddr + (imm << 1);

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;

            if (TTraits.Load)
            {
                Registers[rd] = Read16Rotated(target, PipelineAccess.NonSequential);
                // TODO: wait state
            }
            else
                Bus.Write16(target, (ushort)Registers[rd], PipelineAccess.NonSequential);
        }


        [TemplateParameter<byte>("Operation", size: 2, hi: 11, lo: 10)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadStoreSigned)]
        internal void Thumb_LoadStoreSigned<TTraits>(ushort opcode)
            where TTraits : IThumb_LoadStoreSigned_Traits
        {
            uint rd = (uint)opcode & 0b111;

            uint @base  = Registers[(opcode >> 3) & 0b111];
            uint offset = Registers[(opcode >> 6) & 0b111];
            uint target = @base + offset;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.NonSequential | PipelineAccess.Code;

            switch (TTraits.Operation & 0b11)
            {
                case 0b00: // STRH
                    Bus.Write16(target, (ushort)Registers[rd], PipelineAccess.NonSequential);
                    break;
                case 0b01: // LDSB
                    Registers[rd] = Read8Extended(target, PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
                case 0b10: // LDRH
                    Registers[rd] = Read16Rotated(target, PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
                case 0b11: // LDSH
                    Registers[rd] = Read16Extended(target, PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
            }
        }
    }
}