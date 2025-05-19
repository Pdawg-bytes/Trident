using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.CPU.Decoding;
using Trident.Core.Enums;

using static Trident.Core.CPU.Conditions;

namespace Trident.Core.CPU
{
    public unsafe class ARM7TDMI
    {
        private RegisterSet _regs;

        private uint[] prefetchBuffer = new uint[2];

        public ARM7TDMI()
        {
            _regs = new();
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
            uint opcode = prefetchBuffer[0];
            prefetchBuffer[0] = prefetchBuffer[1];
            prefetchBuffer[1] = 0;
            _regs.PC += 2;

            // ExecuteThumb(opcode); // if needed
        }

        private void StepARM()
        {
            uint opcode = prefetchBuffer[0];
            prefetchBuffer[0] = prefetchBuffer[1];
            prefetchBuffer[1] = 0;
            _regs.PC += 4;

            uint cond = opcode >> 28;
            if (cond != CondAL && !ConditionMet(cond, (int)_regs.CPSR >> 28))
                return;

            ARMInstruction instr = ARMDecoder.GetInstruction(opcode);
            instr(this, opcode);
        }

        public void Run()
        {

        }

        public void Reset()
        {
            _regs.ResetRegisters();
            _regs.SwitchMode(Mode.System);
        }
    }
}