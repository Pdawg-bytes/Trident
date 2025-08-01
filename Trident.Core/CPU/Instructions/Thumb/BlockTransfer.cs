using System.Numerics;
using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Pipeline;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.Thumb;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateParameter<bool>("Load", bit: 11)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.LoadStoreMultiple)]
        internal void Thumb_LoadStoreMultiple<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_LoadStoreMultiple_Traits
        {
            uint regList = (byte)opcode;
            uint rb = ((uint)opcode >> 8) & 0b111;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

            uint address = Registers[rb];

            // Handle when the register list is empty
            if (regList == 0)
            {
                if (TTraits.Load)
                {
                    Registers.PC = Bus.Read32(Registers[rb], PipelineAccess.NonSequential);
                    ReloadPipelineThumb();
                }
                else
                    Bus.Write32(Registers[rb], Registers.PC, PipelineAccess.NonSequential);

                Registers[rb] += 0x40;
                return;
            }


            PerformThumbBlockTransfer
            (
                isLoad: TTraits.Load,
                regList: regList,
                address: ref address,
                rb: rb,
                displacement: (uint)(BitOperations.PopCount(regList) << 2),
                updateRb: true
            );

            // TODO: wait state on load

            // During a load, the final address is written back to Rb if it's not in Rlist.
            if (TTraits.Load && !regList.IsBitSet((int)rb))
                Registers[rb] = address;
        }


        [TemplateParameter<bool>("Pop", bit: 11)]
        [TemplateParameter<bool>("R", bit: 8)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.PushPop)]
        internal void Thumb_PushPop<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_PushPop_Traits
        {
            byte regList = (byte)opcode;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;

            uint address = Registers.SP;

            // Handle when the register list is empty and R is not set
            if (regList == 0 && !TTraits.R)
            {
                if (TTraits.Pop)
                {
                    Registers.PC = Bus.Read32(Registers.SP, PipelineAccess.NonSequential);
                    ReloadPipelineThumb();
                    Registers.SP += 0x40;
                }
                else
                {
                    Registers.SP -= 0x40;
                    Bus.Write32(Registers.SP, Registers.PC, PipelineAccess.NonSequential);
                }
                return;
            }


            if (TTraits.Pop)
            {
                PerformThumbBlockTransfer
                (
                    isLoad: true,
                    regList: regList,
                    address: ref address
                );

                if (TTraits.R)
                {
                    Registers.PC = Bus.Read32(address, PipelineAccess.NonSequential) & 0xFFFFFFFE;
                    address += 4;
                    ReloadPipelineThumb();
                }

                // TODO: wait state

                Registers.SP = address;
            }
            else
            {
                address -= ((uint)BitOperations.PopCount(regList) << 2) + (TTraits.R ? (uint)4 : 0);
                Registers.SP = address;

                PerformThumbBlockTransfer
                (
                    isLoad: false,
                    regList: regList,
                    address: ref address
                );

                if (TTraits.R)
                    Bus.Write32(address, Registers.LR, PipelineAccess.NonSequential);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PerformThumbBlockTransfer(
            bool isLoad,
            uint regList,
            ref uint address,
            uint rb = 0,
            uint displacement = 0,
            bool updateRb = false)
        {
            bool firstTransfer = true;
            PipelineAccess access = PipelineAccess.NonSequential;

            ref uint regBase = ref Registers.GetRegisterRef(0);
            while (regList != 0)
            {
                int index = BitOperations.TrailingZeroCount(regList);

                if (isLoad)
                    Unsafe.Add(ref regBase, index) = Bus.Read32(address, access);
                else
                {
                    Bus.Write32(address, Unsafe.Add(ref regBase, index), access);

                    if (firstTransfer && updateRb)
                    {
                        firstTransfer = false; 
                        Registers[rb] = address + displacement;
                    }
                }

                access = PipelineAccess.Sequential;

                address += 4;
                regList ^= 1u << index;
            }
        }
    }
}