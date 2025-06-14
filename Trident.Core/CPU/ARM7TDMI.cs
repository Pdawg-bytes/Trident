using Trident.Core.Bus;
using Trident.Core.Enums;
using Trident.Core.CPU.Decoding.ARM;
using Trident.Core.CPU.Decoding.Thumb;
using System.Runtime.CompilerServices;

using static Trident.Core.CPU.Conditions;

namespace Trident.Core.CPU
{
    public class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        public RegisterSet Registers;
        private Pipeline _pipeline;
        private TBus _bus;

        private readonly ARMDispatcher<TBus> _armDispatcher;

        private readonly ThumbDispatcher<TBus> _thumbDispatcher;
        private ThumbArguments _thumbParams;

        public ARM7TDMI()
        {
            Registers = new();
            _pipeline = new();

            _armDispatcher = new(this);
            _thumbDispatcher = new(this);
        }

        internal void AttachBus(TBus bus) => _bus = bus;


        public void Reset()
        {
            Registers.ResetRegisters();
            Registers.SetFlag(Flags.F);
            Registers.SwitchMode(PrivilegeMode.Supervisor);
            Registers.SPSR = Registers.CPSR;
            ReloadPipelineARM();
        }

        public void Run()
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Step()
        {
            uint opcode = _pipeline.Prefetch[0];
            _pipeline.Prefetch[0] = _pipeline.Prefetch[1];
            _pipeline.Address[0] = _pipeline.Address[1];

            if (Registers.IsFlagSet(Flags.T))
                StepThumb(opcode);
            else
                StepARM(opcode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StepThumb(uint opcode)
        {
            uint pc = Registers.PC;
            _pipeline.Prefetch[1] = _bus.Read16(pc, _pipeline.Access);
            _pipeline.Address[1] = pc;
            Registers.PC += 2;

            ThumbMetadata instr = _thumbDispatcher.GetInstruction(opcode);
            instr.ArgDecoder(ref _thumbParams, opcode);
            instr.Handler(ref _thumbParams);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StepARM(uint opcode)
        {
            uint pc = Registers.PC;
            _pipeline.Prefetch[1] = _bus.Read32(pc, _pipeline.Access);
            _pipeline.Address[1] = pc;
            Registers.PC += 4;

            uint cond = opcode >> 28;
            if (cond != CondAL && !ConditionMet(cond, (int)Registers.CPSR >> 28))
            {
                _pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
                return;
            }

            ARMInstruction instr = _armDispatcher.GetInstruction(opcode);
            instr(opcode);
        }

        private void ReloadPipelineThumb()
        {
            uint pc = Registers.PC;
            uint pcNext = pc + 2;
            _pipeline.Prefetch[0] = _bus.Read32(pc, PipelineAccess.Code | PipelineAccess.NonSequential);
            _pipeline.Prefetch[1] = _bus.Read32(pcNext, PipelineAccess.Code | PipelineAccess.Sequential);
            _pipeline.Address[0] = pc;
            _pipeline.Address[1] = pcNext;
            _pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
            Registers.PC += 4;
        }

        private void ReloadPipelineARM()
        {
            uint pc = Registers.PC;
            uint pcNext = pc + 4;
            _pipeline.Prefetch[0] = _bus.Read32(pc, PipelineAccess.Code | PipelineAccess.NonSequential);
            _pipeline.Prefetch[1] = _bus.Read32(pcNext, PipelineAccess.Code | PipelineAccess.Sequential);
            _pipeline.Address[0] = pc;
            _pipeline.Address[1] = pcNext;
            _pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
            Registers.PC += 8;
        }


        internal uint NonImplementedARMInstr(uint opcode) => throw new NotImplementedException("This ARM instruction group is not implemented.");

        internal uint NonImplementedThumbInstr(ref ThumbArguments args) => throw new NotImplementedException("This Thumb instruction group is not implemented.");
    }
}