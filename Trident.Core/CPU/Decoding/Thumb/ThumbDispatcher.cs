using static Trident.Core.CPU.Decoding.Thumb.ThumbArgumentHandlers;

namespace Trident.Core.CPU.Decoding.Thumb
{
    internal unsafe static class ThumbDispatcher
    {
        private const int THUMB_DISPATCH_COUNT = 0xFF;
        private static ThumbInstruction[] _thumbInstructionTable = new ThumbInstruction[THUMB_DISPATCH_COUNT];
        private static ThumbArgumentDecoder[] _thumbArgDecoders = new ThumbArgumentDecoder[THUMB_DISPATCH_COUNT];

        private static ThumbMetadata _cachedThumbInstruction;

        private static readonly ThumbDecodePattern[] _instructionPatterns =
        [
            new(mask: ThumbMask.ADD_SUB,         expected: ThumbExpected.ADD_SUB,         handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.SHIFT_IMM,       expected: ThumbExpected.SHIFT_IMM,       handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.MOV_CMP_ADD_SUB, expected: ThumbExpected.MOV_CMP_ADD_SUB, handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.DP_REG,          expected: ThumbExpected.DP_REG,          handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.BX,              expected: ThumbExpected.BX,              handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.HIGH_REG_OPS,    expected: ThumbExpected.HIGH_REG_OPS,    handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.LDR_PC_REL,      expected: ThumbExpected.LDR_PC_REL,      handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.LDRH_STRH_REG,   expected: ThumbExpected.LDRH_STRH_REG,   handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.LDRSB_LDRSH_REG, expected: ThumbExpected.LDRSB_LDRSH_REG, handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.LDR_STR_REG,     expected: ThumbExpected.LDR_STR_REG,     handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.LDRB_STRB_REG,   expected: ThumbExpected.LDRB_STRB_REG,   handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.LDR_STR_IMM,     expected: ThumbExpected.LDR_STR_IMM,     handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.LDRB_STRB_IMM,   expected: ThumbExpected.LDRB_STRB_IMM,   handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.LDRH_STRH_IMM,   expected: ThumbExpected.LDRH_STRH_IMM,   handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.LDR_STR_SP_REL,  expected: ThumbExpected.LDR_STR_SP_REL,  handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.ADD_SP_PC,       expected: ThumbExpected.ADD_SP_PC,       handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.ADD_SUB_SP,      expected: ThumbExpected.ADD_SUB_SP,      handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.PUSH_POP,        expected: ThumbExpected.PUSH_POP,        handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.LDM_STM,         expected: ThumbExpected.LDM_STM,         handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.SWI,             expected: ThumbExpected.SWI,             handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.UNDEFINED_BCC,   expected: ThumbExpected.UNDEFINED_BCC,   handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.BCC,             expected: ThumbExpected.BCC,             handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.B_UNCOND,        expected: ThumbExpected.B_UNCOND,        handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.BL_BLX_PREFIX,   expected: ThumbExpected.BL_BLX_PREFIX,   handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
            new(mask: ThumbMask.BL_SUFFIX,       expected: ThumbExpected.BL_SUFFIX,       handler: &NonImplementedInstr, argDecoder: &NonImplementedArgHandler),
        ];



        /// <summary>
        /// Initializes the Thumb decoder table based on the instructions' 8-bit format.
        /// </summary>
        internal static void InitDecoder()
        {
            for (uint instr = 0; instr < THUMB_DISPATCH_COUNT; instr++)
            {
                _thumbInstructionTable[instr] = &NonImplementedInstr; // Default
                _thumbArgDecoders[instr] = &NonImplementedArgHandler;

                foreach (var pattern in _instructionPatterns)
                {
                    if ((instr & (uint)pattern.Mask) == (uint)pattern.Expected)
                    {
                        _thumbInstructionTable[instr] = pattern.Handler;
                        _thumbArgDecoders[instr] = pattern.ParamDecoder;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the handler and argument decoder for the current Thumb instruction.
        /// </summary>
        /// <param name="opcode">The Thumb instruction to return the handler of.</param>
        /// <returns>A delegate that points to the handler of the instruction, and another point to its respective argument decoder.</returns>
        internal static ref ThumbMetadata GetInstruction(uint opcode)
        {
            uint index = (opcode & 0xFF00) >> 8;

            _cachedThumbInstruction.Handler = _thumbInstructionTable[index];
            _cachedThumbInstruction.ArgDecoder = _thumbArgDecoders[index];

            return ref _cachedThumbInstruction;
        }

        private static uint NonImplementedInstr(ARM7TDMI cpu, ref ThumbArguments args) => throw new NotImplementedException("This Thumb instruction group is not implemented.");
    }
}