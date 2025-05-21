using Trident.Core.Enums;
using Trident.Core.CPU.Decoding.ARM;
using Trident.Core.CPU.Decoding.Thumb;
using System.Runtime.CompilerServices;

using static Trident.Core.CPU.Conditions;

namespace Trident.Core.CPU
{
    public unsafe class ARM7TDMI
    {
        private RegisterSet _regs;
        private Pipeline _pipeline;

        private ThumbArguments _thumbParams;

        private ulong _cycles = 0;

        public ARM7TDMI()
        {
            _regs = new();
            _pipeline = new();
            ARMDispatcher.InitDecoder();
            ThumbDispatcher.InitDecoder();
        }

        public void Reset()
        {
            _regs.ResetRegisters();
            _regs.SetFlag(Flags.F);
            _regs.SwitchMode(PrivilegeMode.Supervisor);
            _regs.SPSR = _regs.CPSR;
            FlushPipeline();
        }

        public void Run()
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Step()
        {
            if (_regs.IsFlagSet(Flags.T))
                StepThumb();
            else
                StepARM();
        }

        private void StepThumb()
        {
            uint opcode = _pipeline.Prefetch[0];
            _pipeline.Prefetch[0] = _pipeline.Prefetch[1];
            _pipeline.Prefetch[1] = 0; // Dummy load
            _regs.PC += 2;

            ThumbMetadata instr = ThumbDispatcher.GetInstruction(opcode);
            instr.ArgDecoder(ref _thumbParams, opcode);
            instr.Handler(this, ref _thumbParams);
        }

        private void StepARM()
        {
            uint opcode = _pipeline.Prefetch[0];
            _pipeline.Prefetch[0] = _pipeline.Prefetch[1];
            _pipeline.Prefetch[1] = 0; // Dummy load
            _regs.PC += 4;

            uint cond = opcode >> 28;
            if (cond != CondAL && !ConditionMet(cond, (int)_regs.CPSR >> 28))
                return;

            ARMInstruction instr = ARMDispatcher.GetInstruction(opcode);
            instr(this, opcode);
        }

        private ulong FlushPipeline()
        {
            if (_regs.IsFlagSet(Flags.T))
            {
                _pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;
                _pipeline.Prefetch[0] = 0; // Dummy load
                _pipeline.Address[0] = _regs.PC;
                _regs.PC += 2;

                _pipeline.Access |= PipelineAccess.Sequential;
                _pipeline.Prefetch[1] = 0; // Dummy load
                _pipeline.Address[1] = _regs.PC;
                _regs.PC += 2;
            }
            else
            {
                _pipeline.Access = PipelineAccess.Code | PipelineAccess.NonSequential;
                _pipeline.Prefetch[0] = 0; // Dummy load
                _pipeline.Address[0] = _regs.PC;
                _regs.PC += 4;

                _pipeline.Access |= PipelineAccess.Sequential;
                _pipeline.Prefetch[1] = 0; // Dummy load
                _pipeline.Address[1] = _regs.PC;
                _regs.PC += 4;
            }

            return 0;
        }
    }
}