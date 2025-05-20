namespace Trident.Core.CPU.Decoding.ARM
{
    internal unsafe static class ARMDispatcher
    {
        private const int ARM_DISPATCH_COUNT = 0x1000;
        private static ARMInstruction[] _armInstructionTable = new ARMInstruction[ARM_DISPATCH_COUNT];

        private static readonly ARMDecodePattern[] _instructionPatterns =
        [
            new(mask: ARMMask.MUL,          expected: ARMExpected.MUL,          handler: &NonImplementedInstr),
            new(mask: ARMMask.MULL,         expected: ARMExpected.MULL,         handler: &NonImplementedInstr),
            new(mask: ARMMask.SWP,          expected: ARMExpected.SWP,          handler: &NonImplementedInstr),
            new(mask: ARMMask.LDRH_STRH,    expected: ARMExpected.LDRH_STRH,    handler: &NonImplementedInstr),
            new(mask: ARMMask.LDRSB_LDRSH,  expected: ARMExpected.LDRSB_LDRSH,  handler: &NonImplementedInstr),
            new(mask: ARMMask.MRS,          expected: ARMExpected.MRS,          handler: &NonImplementedInstr),
            new(mask: ARMMask.MSR_REG,      expected: ARMExpected.MSR_REG,      handler: &NonImplementedInstr),
            new(mask: ARMMask.MSR_IMM,      expected: ARMExpected.MSR_IMM,      handler: &NonImplementedInstr),
            new(mask: ARMMask.BX,           expected: ARMExpected.BX,           handler: &NonImplementedInstr),
            new(mask: ARMMask.DP_IMM_SHIFT, expected: ARMExpected.DP_IMM_SHIFT, handler: &NonImplementedInstr),
            new(mask: ARMMask.DP_REG_SHIFT, expected: ARMExpected.DP_REG_SHIFT, handler: &NonImplementedInstr),
            new(mask: ARMMask.UNDEFINED,    expected: ARMExpected.UNDEFINED,    handler: &NonImplementedInstr),
            new(mask: ARMMask.DP_IMM,       expected: ARMExpected.DP_IMM,       handler: &NonImplementedInstr),
            new(mask: ARMMask.LDR_STR_IMM,  expected: ARMExpected.LDR_STR_IMM,  handler: &NonImplementedInstr),
            new(mask: ARMMask.LDR_STR_REG,  expected: ARMExpected.LDR_STR_REG,  handler: &NonImplementedInstr),
            new(mask: ARMMask.LDM_STM,      expected: ARMExpected.LDM_STM,      handler: &NonImplementedInstr),
            new(mask: ARMMask.B_BL,         expected: ARMExpected.B_BL,         handler: &NonImplementedInstr),
            new(mask: ARMMask.STC_LDC,      expected: ARMExpected.STC_LDC,      handler: &NonImplementedInstr),
            new(mask: ARMMask.CDP,          expected: ARMExpected.CDP,          handler: &NonImplementedInstr),
            new(mask: ARMMask.MCR_MRC,      expected: ARMExpected.MCR_MRC,      handler: &NonImplementedInstr),
            new(mask: ARMMask.SWI,          expected: ARMExpected.SWI,          handler: &NonImplementedInstr),
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
                    if ((instr & (uint)pattern.Mask) == (uint)pattern.Expected)
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