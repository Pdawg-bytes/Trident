using Trident.Core.Bus;
using static Trident.Core.CPU.Decoding.ARM.ARMDecodeMasks;

namespace Trident.Core.CPU.Decoding.ARM
{
    internal partial class ARMDispatcher<TBus> where TBus : struct, IDataBus
    {
        private const int ARM_DISPATCH_COUNT = 0x1000;
        private readonly ARMInstructionDelegate[] _instructionHandlers = new ARMInstructionDelegate[ARM_DISPATCH_COUNT];
        private ARMDecodePattern[] _decodePatterns;

        private readonly ARM7TDMI<TBus> _cpu;

        public ARMDispatcher(ARM7TDMI<TBus> cpu)
        {
            _cpu = cpu;
            InitPatterns();
            InitDecoder();
            InitGeneratedHandlers();
        }

        /// <summary>
        /// Gets the handler for the current ARM instruction.
        /// </summary>
        /// <param name="opcode">The ARM instruction to return the handler of.</param>
        /// <returns>A delegate that points to the handler of the instruction.</returns>
        internal ARMInstruction GetInstruction(uint opcode) =>
            _instructionHandlers[(opcode & 0x0FF00000) >> 16 | (opcode & 0x00F0) >> 4];


        private void InitPatterns()
        {
            _decodePatterns =
            [
                new(mask: MUL_MASK,          expected: MUL_EXPECTED,          handler: _cpu.NonImplementedARMInstr),
                new(mask: MULL_MASK,         expected: MULL_EXPECTED,         handler: _cpu.NonImplementedARMInstr),
                //new(mask: SWP_MASK,          expected: SWP_EXPECTED,          handler: _cpu.ARM_Swap<ARM_Swap_ByteModeFalse>),
                new(mask: LDRH_STRH_MASK,    expected: LDRH_STRH_EXPECTED,    handler: _cpu.NonImplementedARMInstr),
                new(mask: LDRSB_LDRSH_MASK,  expected: LDRSB_LDRSH_EXPECTED,  handler: _cpu.NonImplementedARMInstr),
                new(mask: MRS_MASK,          expected: MRS_EXPECTED,          handler: _cpu.NonImplementedARMInstr),
                new(mask: MSR_REG_MASK,      expected: MSR_REG_EXPECTED,      handler: _cpu.NonImplementedARMInstr),
                new(mask: MSR_IMM_MASK,      expected: MSR_IMM_EXPECTED,      handler: _cpu.NonImplementedARMInstr),
                new(mask: BX_MASK,           expected: BX_EXPECTED,           handler: _cpu.ARM_BranchExchange),
                new(mask: DP_IMM_SHIFT_MASK, expected: DP_IMM_SHIFT_EXPECTED, handler: _cpu.NonImplementedARMInstr),
                new(mask: DP_REG_SHIFT_MASK, expected: DP_REG_SHIFT_EXPECTED, handler: _cpu.NonImplementedARMInstr),
                new(mask: UNDEFINED_MASK,    expected: UNDEFINED_EXPECTED,    handler: _cpu.ARM_Undefined),
                new(mask: DP_IMM_MASK,       expected: DP_IMM_EXPECTED,       handler: _cpu.NonImplementedARMInstr),
                new(mask: LDR_STR_IMM_MASK,  expected: LDR_STR_IMM_EXPECTED,  handler: _cpu.NonImplementedARMInstr),
                new(mask: LDR_STR_REG_MASK,  expected: LDR_STR_REG_EXPECTED,  handler: _cpu.NonImplementedARMInstr),
                new(mask: LDM_STM_MASK,      expected: LDM_STM_EXPECTED,      handler: _cpu.NonImplementedARMInstr),
                new(mask: B_BL_MASK,         expected: B_BL_EXPECTED,         handler: _cpu.ARM_BranchWithLink),
                new(mask: STC_LDC_MASK,      expected: STC_LDC_EXPECTED,      handler: _cpu.NonImplementedARMInstr),
                new(mask: CDP_MASK,          expected: CDP_EXPECTED,          handler: _cpu.NonImplementedARMInstr),
                new(mask: MCR_MRC_MASK,      expected: MCR_MRC_EXPECTED,      handler: _cpu.NonImplementedARMInstr),
                new(mask: SWI_MASK,          expected: SWI_EXPECTED,          handler: _cpu.ARM_SoftwareInterrupt),
            ];
        }

        private void InitDecoder()
        {
            for (uint instr = 0; instr < ARM_DISPATCH_COUNT; instr++)
            {
                _instructionHandlers[instr] = _cpu.NonImplementedARMInstr; // Default

                foreach (var pattern in _decodePatterns)
                {
                    if ((instr & pattern.Mask) == pattern.Expected)
                    {
                        _instructionHandlers[instr] = pattern.Handler;
                        break;
                    }
                }
            }
        }
    }
}