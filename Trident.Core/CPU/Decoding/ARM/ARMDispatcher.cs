using Trident.Core.Bus;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU.Decoding.ARM
{
    internal partial struct ARMDispatcher<TBus> where TBus : struct, IDataBus
    {
        private const int ARMDispatchCount = 0x1000;
        private readonly ARMInstructionDelegate[] _instructionHandlers = new ARMInstructionDelegate[ARMDispatchCount];

        private readonly ARM7TDMI<TBus> _cpu;

        public ARMDispatcher(ARM7TDMI<TBus> cpu)
        {
            _cpu = cpu;

            foreach (var i in Enumerable.Range(0, ARMDispatchCount))
                _instructionHandlers[i] = _cpu.NonImplementedARMInstr;

            InitGeneratedHandlers();
        }

        /// <summary>
        /// Gets the handler for the current ARM instruction.
        /// </summary>
        /// <param name="opcode">The ARM instruction to return the handler of.</param>
        /// <returns>A delegate that points to the handler of the instruction.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ARMInstruction GetInstruction(uint opcode) =>
            _instructionHandlers[(opcode & 0x0FF00000) >> 16 | (opcode & 0x00F0) >> 4];
    }
}