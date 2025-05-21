using static Trident.Core.CPU.Decoding.ARM.ARMDecodeMasks;

namespace Trident.Core.CPU.Decoding.ARM
{
    internal unsafe static class ARMDispatcher
    {
        private const int ARM_DISPATCH_COUNT = 0x1000;
        private static ARMInstruction[] _armInstructionTable = new ARMInstruction[ARM_DISPATCH_COUNT];

        private static readonly ARMDecodePattern[] _instructionPatterns =
        [
            new(mask: MUL_MASK,          expected: MUL_EXPECTED,          handler: &NonImplementedInstr),
            new(mask: MULL_MASK,         expected: MULL_EXPECTED,         handler: &NonImplementedInstr),
            new(mask: SWP_MASK,          expected: SWP_EXPECTED,          handler: &NonImplementedInstr),
            new(mask: LDRH_STRH_MASK,    expected: LDRH_STRH_EXPECTED,    handler: &NonImplementedInstr),
            new(mask: LDRSB_LDRSH_MASK,  expected: LDRSB_LDRSH_EXPECTED,  handler: &NonImplementedInstr),
            new(mask: MRS_MASK,          expected: MRS_EXPECTED,          handler: &NonImplementedInstr),
            new(mask: MSR_REG_MASK,      expected: MSR_REG_EXPECTED,      handler: &NonImplementedInstr),
            new(mask: MSR_IMM_MASK,      expected: MSR_IMM_EXPECTED,      handler: &NonImplementedInstr),
            new(mask: BX_MASK,           expected: BX_EXPECTED,           handler: &NonImplementedInstr),
            new(mask: DP_IMM_SHIFT_MASK, expected: DP_IMM_SHIFT_EXPECTED, handler: &NonImplementedInstr),
            new(mask: DP_REG_SHIFT_MASK, expected: DP_REG_SHIFT_EXPECTED, handler: &NonImplementedInstr),
            new(mask: UNDEFINED_MASK,    expected: UNDEFINED_EXPECTED,    handler: &NonImplementedInstr),
            new(mask: DP_IMM_MASK,       expected: DP_IMM_EXPECTED,       handler: &NonImplementedInstr),
            new(mask: LDR_STR_IMM_MASK,  expected: LDR_STR_IMM_EXPECTED,  handler: &NonImplementedInstr),
            new(mask: LDR_STR_REG_MASK,  expected: LDR_STR_REG_EXPECTED,  handler: &NonImplementedInstr),
            new(mask: LDM_STM_MASK,      expected: LDM_STM_EXPECTED,      handler: &NonImplementedInstr),
            new(mask: B_BL_MASK,         expected: B_BL_EXPECTED,         handler: &NonImplementedInstr),
            new(mask: STC_LDC_MASK,      expected: STC_LDC_EXPECTED,      handler: &NonImplementedInstr),
            new(mask: CDP_MASK,          expected: CDP_EXPECTED,          handler: &NonImplementedInstr),
            new(mask: MCR_MRC_MASK,      expected: MCR_MRC_EXPECTED,      handler: &NonImplementedInstr),
            new(mask: SWI_MASK,          expected: SWI_EXPECTED,          handler: &NonImplementedInstr),
        ];


        /// <summary>
        /// Initializes the ARMv4 decoder table based on the instructions' 12-bit format.
        /// </summary>
        internal static void InitDecoder()
        {
            for (uint instr = 0; instr < ARM_DISPATCH_COUNT; instr++)
            {
                _armInstructionTable[instr] = &NonImplementedInstr; // Default

                foreach (var pattern in _instructionPatterns)
                {
                    if ((instr & pattern.Mask) == pattern.Expected)
                    {
                        _armInstructionTable[instr] = pattern.Handler;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the handler for the current ARM instruction.
        /// </summary>
        /// <param name="opcode">The ARM instruction to return the handler of.</param>
        /// <returns>A delegate that points to the handler of the instruction.</returns>
        internal static ARMInstruction GetInstruction(uint opcode) =>
            _armInstructionTable[(opcode & 0x0FF00000) >> 16 | (opcode & 0x00F0) >> 4];

        private static uint NonImplementedInstr(ARM7TDMI cpu, uint opcode) => throw new NotImplementedException("This ARM instruction group is not implemented.");
    }
}