namespace Trident.Core.CPU.Decoding
{
    internal unsafe static class ARMDecoder
    {
        private const int ARM_DISPATCH_COUNT = 0x1000;
        private static ARMInstruction[] _armInstructionTable = new ARMInstruction[ARM_DISPATCH_COUNT];

        struct ARMInstructionPattern
        {
            internal uint Mask;
            internal uint Expected;
            internal ARMInstruction Handler;
        }

        private static readonly ARMInstructionPattern[] _instructionPatterns =
        {
            new() { Mask = 0b111111001111, Expected = 0b000000001001, Handler = &DummyInstruction }, // MUL, MLA
            new() { Mask = 0b111110001111, Expected = 0b000010001001, Handler = &DummyInstruction }, // MULL, MLAL
            new() { Mask = 0b111110111111, Expected = 0b000100001001, Handler = &DummyInstruction }, // SWP
            new() { Mask = 0b111000001111, Expected = 0b000000001011, Handler = &DummyInstruction }, // LDRH, STRH
            new() { Mask = 0b111000011101, Expected = 0b000000011101, Handler = &DummyInstruction }, // LDRSB, LDRSH
            new() { Mask = 0b111110111111, Expected = 0b000100000000, Handler = &DummyInstruction }, // MRS
            new() { Mask = 0b111110111111, Expected = 0b000100100000, Handler = &DummyInstruction }, // MSR (register)
            new() { Mask = 0b111110110000, Expected = 0b001100100000, Handler = &DummyInstruction }, // MSR (immediate)
            new() { Mask = 0b111111111111, Expected = 0b000100100001, Handler = &DummyInstruction }, // BX
            new() { Mask = 0b111000000001, Expected = 0b000000000000, Handler = &DummyInstruction }, // Data Processing (immediate shift)
            new() { Mask = 0b111000001001, Expected = 0b000000000001, Handler = &DummyInstruction }, // Data Processing (register shift)
            new() { Mask = 0b111110110000, Expected = 0b001100000000, Handler = &DummyInstruction }, // Undefined instruction
            new() { Mask = 0b111000000000, Expected = 0b001000000000, Handler = &DummyInstruction }, // Data Processing (immediate value)
            new() { Mask = 0b111000000000, Expected = 0b010000000000, Handler = &DummyInstruction }, // LDR, STR (immediate offset)
            new() { Mask = 0b111000000001, Expected = 0b011000000000, Handler = &DummyInstruction }, // LDR, STR (register offset)
            new() { Mask = 0b111000000000, Expected = 0b100000000000, Handler = &DummyInstruction }, // LDM, STM
            new() { Mask = 0b111000000000, Expected = 0b101000000000, Handler = &DummyInstruction }, // B, BL
            new() { Mask = 0b111000000000, Expected = 0b110000000000, Handler = &DummyInstruction }, // STC, LDC
            new() { Mask = 0b111100000001, Expected = 0b111000000000, Handler = &DummyInstruction }, // CDP
            new() { Mask = 0b111100000001, Expected = 0b111000000001, Handler = &DummyInstruction }, // MCR, MRC
            new() { Mask = 0b111100000000, Expected = 0b111100000000, Handler = &DummyInstruction }  // SWI
        };

        /// <summary>
        /// Initializes the ARMv4 decoder table based on the instructions' 12-bit format.
        /// </summary>
        internal static void InitARMDecoder()
        {
            for (uint instr = 0; instr < ARM_DISPATCH_COUNT; instr++)
            {
                foreach (var pattern in _instructionPatterns)
                {
                    if ((instr & pattern.Mask) == pattern.Expected)
                        _armInstructionTable[instr] = pattern.Handler;
                    else
                        _armInstructionTable[instr] = &DummyInstruction;
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

        private static uint DummyInstruction(ARM7TDMI cpu, uint opcode) => 0;
    }
}