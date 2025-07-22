using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.Thumb;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadPCRelative)]
        internal void Thumb_LoadPCRelative(ushort opcode)
        {
            uint immOffset = (uint)opcode & 0xFF;
            uint address = Registers.PC.Align<uint>() + (immOffset << 2);

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

            Registers[(opcode >> 8) & 0b111] = Bus.Read32(address, PipelineAccess.NonSequential);
            // TODO: wait state
        }


        [TemplateParameter<bool>("Load", bit: 11)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadStoreSPRelative)]
        internal void Thumb_LoadStoreSPRelative<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_LoadStoreSPRelative_Traits
        {
            uint rd = ((uint)opcode >> 8) & 0b111;

            uint immOffset = (uint)opcode & 0xFF;
            uint address = Registers.SP + (immOffset << 2);

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

            if (TTraits.Load)
            {
                Registers[rd] = Read32Rotated(address, PipelineAccess.NonSequential);
                // TODO: wait state
            }
            else
                Bus.Write32(address, Registers[rd], PipelineAccess.NonSequential);
        }


        [TemplateParameter<byte>("Operation", size: 2, hi: 11, lo: 10)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadStoreRegOffset)]
        internal void Thumb_LoadStoreRegOffset<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_LoadStoreRegOffset_Traits
        {
            uint rd = (uint)opcode & 0b111;

            uint baseAddr = Registers[(opcode >> 3) & 0b111];
            uint offset   = Registers[(opcode >> 6) & 0b111];
            uint address  = baseAddr + offset;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

            switch (TTraits.Operation & 0b11)
            {
                case 0b00: // STR
                    Bus.Write32(address, Registers[rd], PipelineAccess.NonSequential);
                    break;
                case 0b01: // STRB
                    Bus.Write8(address, (byte)Registers[rd], PipelineAccess.NonSequential);
                    break;
                case 0b10: // LDR
                    Registers[rd] = Read32Rotated(address, PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
                case 0b11: // LDRB
                    Registers[rd] = Bus.Read8(address, PipelineAccess.NonSequential);
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

            uint immOffset = (uint)(opcode >> 6) & 0x1F;
            uint baseAddr = Registers[(opcode >> 3) & 0b111];

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

            switch (TTraits.Operation & 0b11)
            {
                case 0b00: // STR
                    Bus.Write32(baseAddr + (immOffset << 2), Registers[rd], PipelineAccess.NonSequential);
                    break;
                case 0b01: // LDR
                    Registers[rd] = Read32Rotated(baseAddr + (immOffset << 2), PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
                case 0b10: // STRB
                    Bus.Write8(baseAddr + immOffset, (byte)Registers[rd], PipelineAccess.NonSequential);
                    break;
                case 0b11: // LDRB
                    Registers[rd] = Bus.Read8(baseAddr + immOffset, PipelineAccess.NonSequential);
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

            uint immOffset = ((uint)opcode >> 6) & 0x1F;
            uint baseAddr = Registers[(opcode >> 3) & 0b111];
            uint address = baseAddr + (immOffset << 1);

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

            if (TTraits.Load)
            {
                Registers[rd] = Read16Rotated(address, PipelineAccess.NonSequential);
                // TODO: wait state
            }
            else
                Bus.Write16(address, (ushort)Registers[rd], PipelineAccess.NonSequential);
        }


        [TemplateParameter<byte>("Operation", size: 2, hi: 11, lo: 10)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadStoreSigned)]
        internal void Thumb_LoadStoreSigned<TTraits>(ushort opcode)
            where TTraits : IThumb_LoadStoreSigned_Traits
        {
            uint rd = (uint)opcode & 0b111;

            uint baseAddr  = Registers[(opcode >> 3) & 0b111];
            uint immOffset = Registers[(opcode >> 6) & 0b111];
            uint address   = baseAddr + immOffset;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

            switch (TTraits.Operation & 0b11)
            {
                case 0b00: // STRH
                    Bus.Write16(address, (ushort)Registers[rd], PipelineAccess.NonSequential);
                    break;
                case 0b01: // LDSB
                    Registers[rd] = Read8Extended(address, PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
                case 0b10: // LDRH
                    Registers[rd] = Read16Rotated(address, PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
                case 0b11: // LDSH
                    Registers[rd] = Read16Extended(address, PipelineAccess.NonSequential);
                    // TODO: wait state
                    break;
            }
        }
    }
}