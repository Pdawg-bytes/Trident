using System.Numerics;
using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.ARM;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU;

public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
{
    [TemplateParameter<bool>("PreIndexed", bit: 24)]
    [TemplateParameter<bool>("AddOffset", bit: 23)]
    [TemplateParameter<bool>("UserMode", bit: 22)]
    [TemplateParameter<bool>("Writeback", bit: 21)]
    [TemplateParameter<bool>("Load", bit: 20)]
    [TemplateGroup<ARMGroup>(ARMGroup.BlockDataTransfer)]
    internal void ARM_BlockDataTransfer<TTraits>(uint opcode)
        where TTraits : struct, IARM_BlockDataTransfer_Traits
    {
        uint regList = (ushort)opcode;
        uint rb = (opcode >> 16) & 0x0F;

        uint address = Registers[rb];

        Registers.PC += 4;
        Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

        // We can use this trick to emulate all LDM/STM addressing modes.
        bool preIndex = TTraits.AddOffset ? TTraits.PreIndexed : !TTraits.PreIndexed;

        // Handle when the register list is empty
        uint transferSize;
        if (regList == 0)
        {
            transferSize = 64;
            regList = 1u << 15;
        }
        else
            transferSize = (uint)BitOperations.PopCount(regList) << 2;

        bool pcIncluded = regList.IsBitSet(15);

        ProcessorMode mode = Registers.CurrentMode;
        bool switchMode = TTraits.UserMode && !RegisterSet.IsUserOrSystem(mode) && (!TTraits.Load || !pcIncluded);
        if (switchMode) Registers.SwitchMode(ProcessorMode.USR);

        uint finalAddress = address + (TTraits.AddOffset ? transferSize : (uint)-transferSize);
        if (!TTraits.AddOffset) address -= transferSize;


        bool firstTransfer = true;
        PipelineAccess access = PipelineAccess.NonSequential;

        while (regList != 0)
        {
            int index = BitOperations.TrailingZeroCount(regList);

            if (preIndex)
                address += 4;

            if (TTraits.Load)
            {
                if (TTraits.Writeback && firstTransfer)
                    Registers[rb] = finalAddress;

                Registers[index] = Bus.Read32(address, access);
            }
            else
            {
                Bus.Write32(address, Registers[index], access);
                if (TTraits.Writeback && firstTransfer)
                    Registers[rb] = finalAddress;
            }

            if (!preIndex)
                address += 4;

            if (firstTransfer)
            {
                firstTransfer = false;
                access = PipelineAccess.Sequential;
            }

            regList ^= 1u << index;
        }


        bool pipelineFlushed = false;
        if (TTraits.Load && pcIncluded)
        {
            // GBATEK: "When S=1: If instruction is LDM and R15 is in the list: (Mode Changes)
            //              While R15 loaded, additionally: CPSR = SPSR_<current mode>"
            if (TTraits.UserMode)
            {
                Flags spsr = Registers.SPSR;
                Registers.SwitchMode((ProcessorMode)(spsr & (Flags)0x1F));
                Registers.CPSR = spsr;
            }

            pipelineFlushed = true;
        }

        if (switchMode)
            Registers.SwitchMode(mode);

        if ((TTraits.Writeback && rb == 15) || pipelineFlushed)
        {
            if (Registers.IsFlagSet(Flags.T)) ReloadPipelineThumb();
            else ReloadPipelineARM();
        }
    }
}