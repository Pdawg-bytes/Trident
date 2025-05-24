using Trident.Core.Bus;
using Trident.Core.Enums;
using Trident.Core.CPU.Decoding.ARM;
using Trident.Core.CPU.Decoding.Thumb;
using System.Runtime.CompilerServices;

using static Trident.Core.CPU.Conditions;

namespace Trident.Core.CPU
{
    public unsafe class ARM7TDMI
    {
        public RegisterSet Registers;
        private Pipeline _pipeline;
        private readonly DataBus _bus;

        private readonly ARMDispatcher _armDispatcher;

        private readonly ThumbDispatcher _thumbDispatcher;
        private ThumbArguments _thumbParams;


        public ARM7TDMI(DataBus bus)
        {
            Registers = new();
            _pipeline = new();

            _bus = bus;
            _bus.AttachComponents(this);

            _armDispatcher = new();
            _thumbDispatcher = new();
        }

        public void Reset()
        {
            Registers.ResetRegisters();
            Registers.SetFlag(Flags.F);
            Registers.SwitchMode(PrivilegeMode.Supervisor);
            Registers.SPSR = Registers.CPSR;
            ReloadPipeline();
        }

        public void Run()
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Step()
        {
            uint opcode = _pipeline.Prefetch[0];
            _pipeline.Prefetch[0] = _pipeline.Prefetch[1];

            if (Registers.IsFlagSet(Flags.T))
                StepThumb(opcode);
            else
                StepARM(opcode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StepThumb(uint opcode)
        {
            _pipeline.Prefetch[1] = 0; // Dummy load
            Registers.PC += 2;

            ThumbMetadata instr = _thumbDispatcher.GetInstruction(opcode);
            instr.ArgDecoder(ref _thumbParams, opcode);
            instr.Handler(this, ref _thumbParams);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StepARM(uint opcode)
        {
            _pipeline.Prefetch[1] = 0; // Dummy load
            Registers.PC += 4;

            uint cond = opcode >> 28;
            if (cond != CondAL && !ConditionMet(cond, (int)Registers.CPSR >> 28))
            {
                _pipeline.Access = PipelineAccess.Code | PipelineAccess.Sequential;
                return;
            }

            ARMInstruction instr = _armDispatcher.GetInstruction(opcode);
            instr(this, opcode);
        }

        private ulong ReloadPipeline()
        {
            if (Registers.IsFlagSet(Flags.T))
            {
                _pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;
                _pipeline.Prefetch[0] = 0; // Dummy load
                _pipeline.Address[0] = Registers.PC;
                Registers.PC += 2;

                _pipeline.Access |= PipelineAccess.Sequential;
                _pipeline.Prefetch[1] = 0; // Dummy load
                _pipeline.Address[1] = Registers.PC;
                Registers.PC += 2;
            }
            else
            {
                _pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;
                _pipeline.Prefetch[0] = 0; // Dummy load
                _pipeline.Address[0] = Registers.PC;
                Registers.PC += 4;

                _pipeline.Access |= PipelineAccess.Sequential;
                _pipeline.Prefetch[1] = 0; // Dummy load
                _pipeline.Address[1] = Registers.PC;
                Registers.PC += 4;
            }

            return 0;
        }
    }
}