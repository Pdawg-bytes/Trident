using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding.ARM;
using Trident.Core.CPU.Decoding.Thumb;
using System.Runtime.CompilerServices;

using static Trident.Core.CPU.Conditions;
using Trident.Core.CPU.Registers;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        public RegisterSet Registers;
        public InstructionPipeline Pipeline;
        public TBus Bus;

        private readonly ARMDispatcher<TBus> _armDispatcher;

        private readonly ThumbDispatcher<TBus> _thumbDispatcher;
        private ThumbArguments _thumbParams = new();

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
            Registers.SetFlag(Flags.F);
            Registers.SwitchMode(PrivilegeMode.Supervisor);
            ReloadPipelineARM();
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
            
            ThumbMetadata instr = _thumbDispatcher.GetInstruction(opcode);
            instr.ArgDecoder(ref _thumbParams, opcode);
            instr.Handler(ref _thumbParams);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StepARM(uint opcode)
        {
            uint pc = Registers.PC;
            Pipeline.Prefetch[1] = Bus.Read32(pc, Pipeline.Access);

            uint cond = opcode >> 28;
            if (cond != CondAL && !ConditionMet(cond, Registers.CPSR))
            {
                Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
                Registers.PC += 4;
                return;
            }

            ARMInstruction instr = _armDispatcher.GetInstruction(opcode);
            instr(opcode);
        }

        private void ReloadPipelineThumb()
        {
            Pipeline.Prefetch[0] = Bus.Read16(Registers.PC, PipelineAccess.Code | PipelineAccess.NonSequential);
            Pipeline.Prefetch[1] = Bus.Read16(Registers.PC + 2, PipelineAccess.Code | PipelineAccess.Sequential);
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
            Registers.PC += 4;
        }

        private void ReloadPipelineARM()
        {
            Pipeline.Prefetch[0] = Bus.Read32(Registers.PC, PipelineAccess.Code | PipelineAccess.NonSequential);
            Pipeline.Prefetch[1] = Bus.Read32(Registers.PC + 4, PipelineAccess.Code | PipelineAccess.Sequential);
            Pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
            Registers.PC += 8;
        }


        internal void NonImplementedARMInstr(uint opcode) => throw new NotImplementedException("This ARM instruction group is not implemented.");
        internal void NonImplementedThumbInstr(ref ThumbArguments args) => throw new NotImplementedException("This Thumb instruction group is not implemented.");
    }
}