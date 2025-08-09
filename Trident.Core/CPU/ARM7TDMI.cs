using System.Numerics;
using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Registers;
using Trident.Core.CPU.Decoding.ARM;
using Trident.Core.CPU.Decoding.Thumb;
using System.Runtime.CompilerServices;

using static Trident.Core.CPU.Conditions;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        public RegisterSet Registers;
        public InstructionPipeline Pipeline;
        public TBus Bus;

        private readonly ARMDispatcher<TBus> _armDispatcher;
        private readonly ThumbDispatcher<TBus> _thumbDispatcher;

        public ARM7TDMI()
        {
            Registers = new();
            Pipeline = new();

            _armDispatcher = new(this);
            _thumbDispatcher = new(this);
        }

        public void AttachBus(TBus bus) => Bus = bus;


        public void Reset()
        {
            Registers.ResetRegisters();
            Registers.SwitchMode(PrivilegeMode.SVC);

            Pipeline.Prefetch[0] = Pipeline.Prefetch[1] = 0xF0000000;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }


        public void Run()
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Step()
        {
            uint opcode = Pipeline.Prefetch[0];
            Pipeline.Prefetch[0] = Pipeline.Prefetch[1];
            Registers.PC &= 0xFFFFFFFE;

            if (Registers.IsFlagSet(Flags.T))
                StepThumb(opcode);
            else
                StepARM(opcode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StepThumb(uint opcode)
        {
            uint pc = Registers.PC;
            Pipeline.Prefetch[1] = Bus.Read16(pc, Pipeline.Access);
            
            ThumbInstruction instr = _thumbDispatcher.GetInstruction((ushort)opcode);
            instr((ushort)opcode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StepARM(uint opcode)
        {
            uint pc = Registers.PC;
            Pipeline.Prefetch[1] = Bus.Read32(pc, Pipeline.Access);

            uint cond = opcode >> 28;
            if (cond != CondAL && !ConditionMet(cond, Registers.CPSR))
            {
                Registers.PC += 4;
                Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
                return;
            }

            ARMInstruction instr = _armDispatcher.GetInstruction(opcode);
            instr(opcode);
        }


        internal void ReloadPipelineThumb()
        {
            Pipeline.Prefetch[0] = Bus.Read16(Registers.PC, PipelineAccess.Code | PipelineAccess.NonSequential);
            Pipeline.Prefetch[1] = Bus.Read16(Registers.PC + 2, PipelineAccess.Code | PipelineAccess.Sequential);
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
            Registers.PC += 4;
        }

        internal void ReloadPipelineARM()
        {
            Pipeline.Prefetch[0] = Bus.Read32(Registers.PC, PipelineAccess.Code | PipelineAccess.NonSequential);
            Pipeline.Prefetch[1] = Bus.Read32(Registers.PC + 4, PipelineAccess.Code | PipelineAccess.Sequential);
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
            Registers.PC += 8;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read16Rotated(uint address, PipelineAccess access)
        {
            uint val = Bus.Read16(address, access);

            return (address & 1) != 0
                ? BitOperations.RotateRight(val, 8)
                : val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read32Rotated(uint address, PipelineAccess access)
        {
            uint val = Bus.Read32(address, access);

            int shift = ((int)address & 0b111) << 3;
            return BitOperations.RotateRight(val, shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read8Extended(uint address, PipelineAccess access) => 
            (uint)((uint)Bus.Read8(address, access)).ExtendFrom(8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read16Extended(uint address, PipelineAccess access)
        {
            uint val = Bus.Read16(address, access);

            return (address & 1) != 0
                ? (uint)(val >> 8).ExtendFrom(8)
                : (uint)val.ExtendFrom(16);
        }


        internal void NonImplementedARMInstr(uint opcode) => throw new NotImplementedException("This ARM instruction group is not implemented.");
        internal void NonImplementedThumbInstr(ushort opcode) => throw new NotImplementedException("This Thumb instruction group is not implemented.");
    }
}