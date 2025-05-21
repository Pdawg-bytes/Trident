using static Trident.Core.CPU.Decoding.Thumb.ThumbArgumentDecoders;

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
            new(mask: ThumbMask.ADD_SUB,         expected: ThumbExpected.ADD_SUB,         handler: &NonImplementedInstr, argDecoder: &HandleAddSub),
            new(mask: ThumbMask.SHIFT_IMM,       expected: ThumbExpected.SHIFT_IMM,       handler: &NonImplementedInstr, argDecoder: &HandleShiftImm),
            new(mask: ThumbMask.MOV_CMP_ADD_SUB, expected: ThumbExpected.MOV_CMP_ADD_SUB, handler: &NonImplementedInstr, argDecoder: &HandleMovCmpAddSub),
            new(mask: ThumbMask.DP_REG,          expected: ThumbExpected.DP_REG,          handler: &NonImplementedInstr, argDecoder: &HandleDataProcReg),
            new(mask: ThumbMask.BX,              expected: ThumbExpected.BX,              handler: &NonImplementedInstr, argDecoder: &HandleBX),
            new(mask: ThumbMask.HIGH_REG_OPS,    expected: ThumbExpected.HIGH_REG_OPS,    handler: &NonImplementedInstr, argDecoder: &HandleHighRegOps),
            new(mask: ThumbMask.LDR_PC_REL,      expected: ThumbExpected.LDR_PC_REL,      handler: &NonImplementedInstr, argDecoder: &HandleLdrPCRel),
            new(mask: ThumbMask.LDRH_STRH_REG,   expected: ThumbExpected.LDRH_STRH_REG,   handler: &NonImplementedInstr, argDecoder: &HandleLdrhStrhReg),
            new(mask: ThumbMask.LDRSH_LDRSB_REG, expected: ThumbExpected.LDRSB_LDRSH_REG, handler: &NonImplementedInstr, argDecoder: &HandleLdrsbLdrshReg),
            new(mask: ThumbMask.LDR_STR_REG,     expected: ThumbExpected.LDR_STR_REG,     handler: &NonImplementedInstr, argDecoder: &HandleLdrStrReg),
            new(mask: ThumbMask.LDRB_STRB_REG,   expected: ThumbExpected.LDRB_STRB_REG,   handler: &NonImplementedInstr, argDecoder: &HandleLdrbStrbReg),
            new(mask: ThumbMask.LDR_STR_IMM,     expected: ThumbExpected.LDR_STR_IMM,     handler: &NonImplementedInstr, argDecoder: &HandleLdrStrImm),
            new(mask: ThumbMask.LDRB_STRB_IMM,   expected: ThumbExpected.LDRB_STRB_IMM,   handler: &NonImplementedInstr, argDecoder: &HandleLdrbStrbImm),
            new(mask: ThumbMask.LDRH_STRH_IMM,   expected: ThumbExpected.LDRH_STRH_IMM,   handler: &NonImplementedInstr, argDecoder: &HandleLdrhStrhImm),
            new(mask: ThumbMask.LDR_STR_SP_REL,  expected: ThumbExpected.LDR_STR_SP_REL,  handler: &NonImplementedInstr, argDecoder: &HandleLdrStrSPRel),
            new(mask: ThumbMask.ADD_SP_PC,       expected: ThumbExpected.ADD_SP_PC,       handler: &NonImplementedInstr, argDecoder: &HandleAddSPPC),
            new(mask: ThumbMask.ADD_SUB_SP,      expected: ThumbExpected.ADD_SUB_SP,      handler: &NonImplementedInstr, argDecoder: &HandleAddSubSP),
            new(mask: ThumbMask.PUSH_POP,        expected: ThumbExpected.PUSH_POP,        handler: &NonImplementedInstr, argDecoder: &HandlePushPop),
            new(mask: ThumbMask.LDM_STM,         expected: ThumbExpected.LDM_STM,         handler: &NonImplementedInstr, argDecoder: &HandleLdmStm),
            new(mask: ThumbMask.SWI,             expected: ThumbExpected.SWI,             handler: &NonImplementedInstr, argDecoder: &HandleSWI),
            new(mask: ThumbMask.UNDEFINED_BCC,   expected: ThumbExpected.UNDEFINED_BCC,   handler: &NonImplementedInstr, argDecoder: &HandleBCC),
            new(mask: ThumbMask.BCC,             expected: ThumbExpected.BCC,             handler: &NonImplementedInstr, argDecoder: &HandleBCC),
            new(mask: ThumbMask.B_UNCOND,        expected: ThumbExpected.B_UNCOND,        handler: &NonImplementedInstr, argDecoder: &HandleBUncond),
            new(mask: ThumbMask.BL_BLX_PREFIX,   expected: ThumbExpected.BL_BLX_PREFIX,   handler: &NonImplementedInstr, argDecoder: &HandleBlBlxPrefix),
            new(mask: ThumbMask.BL_SUFFIX,       expected: ThumbExpected.BL_SUFFIX,       handler: &NonImplementedInstr, argDecoder: &HandleBlSuffix),
        ];


        /// <summary>
        /// Initializes the Thumb decoder table based on the instructions' 8-bit format.
        /// </summary>
        internal static void InitDecoder()
        {
            for (uint instr = 0; instr < THUMB_DISPATCH_COUNT; instr++)
            {
                // Default
                _thumbInstructionTable[instr] = &NonImplementedInstr;
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
        private static void NonImplementedArgHandler(ref ThumbArguments args, uint opcode) => throw new NotImplementedException("This Thumb instruction group does not have an associated argument decoder.");
    }
}