using static Trident.Core.CPU.Decoding.Thumb.ThumbDecodeMasks;
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
            new(mask: ADD_SUB_MASK,         expected: ADD_SUB_EXPECTED,         handler: &NonImplementedInstr, argDecoder: &HandleAddSub),
            new(mask: SHIFT_IMM_MASK,       expected: SHIFT_IMM_EXPECTED,       handler: &NonImplementedInstr, argDecoder: &HandleShiftImm),
            new(mask: MOV_CMP_ADD_SUB_MASK, expected: MOV_CMP_ADD_SUB_EXPECTED, handler: &NonImplementedInstr, argDecoder: &HandleMovCmpAddSub),
            new(mask: DP_REG_MASK,          expected: DP_REG_EXPECTED,          handler: &NonImplementedInstr, argDecoder: &HandleDataProcReg),
            new(mask: BX_MASK,              expected: BX_EXPECTED,              handler: &NonImplementedInstr, argDecoder: &HandleBX),
            new(mask: HIGH_REG_OPS_MASK,    expected: HIGH_REG_OPS_EXPECTED,    handler: &NonImplementedInstr, argDecoder: &HandleHighRegOps),
            new(mask: LDR_PC_REL_MASK,      expected: LDR_PC_REL_EXPECTED,      handler: &NonImplementedInstr, argDecoder: &HandleLdrPCRel),
            new(mask: LDRH_STRH_REG_MASK,   expected: LDRH_STRH_REG_EXPECTED,   handler: &NonImplementedInstr, argDecoder: &HandleLdrhStrhReg),
            new(mask: LDRSH_LDRSB_REG_MASK, expected: LDRSB_LDRSH_REG_EXPECTED, handler: &NonImplementedInstr, argDecoder: &HandleLdrsbLdrshReg),
            new(mask: LDR_STR_REG_MASK,     expected: LDR_STR_REG_EXPECTED,     handler: &NonImplementedInstr, argDecoder: &HandleLdrStrReg),
            new(mask: LDRB_STRB_REG_MASK,   expected: LDRB_STRB_REG_EXPECTED,   handler: &NonImplementedInstr, argDecoder: &HandleLdrbStrbReg),
            new(mask: LDR_STR_IMM_MASK,     expected: LDR_STR_IMM_EXPECTED,     handler: &NonImplementedInstr, argDecoder: &HandleLdrStrImm),
            new(mask: LDRB_STRB_IMM_MASK,   expected: LDRB_STRB_IMM_EXPECTED,   handler: &NonImplementedInstr, argDecoder: &HandleLdrbStrbImm),
            new(mask: LDRH_STRH_IMM_MASK,   expected: LDRH_STRH_IMM_EXPECTED,   handler: &NonImplementedInstr, argDecoder: &HandleLdrhStrhImm),
            new(mask: LDR_STR_SP_REL_MASK,  expected: LDR_STR_SP_REL_EXPECTED,  handler: &NonImplementedInstr, argDecoder: &HandleLdrStrSPRel),
            new(mask: ADD_SP_PC_MASK,       expected: ADD_SP_PC_EXPECTED,       handler: &NonImplementedInstr, argDecoder: &HandleAddSPPC),
            new(mask: ADD_SUB_SP_MASK,      expected: ADD_SUB_SP_EXPECTED,      handler: &NonImplementedInstr, argDecoder: &HandleAddSubSP),
            new(mask: PUSH_POP_MASK,        expected: PUSH_POP_EXPECTED,        handler: &NonImplementedInstr, argDecoder: &HandlePushPop),
            new(mask: LDM_STM_MASK,         expected: LDM_STM_EXPECTED,         handler: &NonImplementedInstr, argDecoder: &HandleLdmStm),
            new(mask: SWI_MASK,             expected: SWI_EXPECTED,             handler: &NonImplementedInstr, argDecoder: &HandleSWI),
            new(mask: UNDEFINED_BCC_MASK,   expected: UNDEFINED_BCC_EXPECTED,   handler: &NonImplementedInstr, argDecoder: &HandleBCC),
            new(mask: BCC_MASK,             expected: BCC_EXPECTED,             handler: &NonImplementedInstr, argDecoder: &HandleBCC),
            new(mask: B_UNCOND_MASK,        expected: B_UNCOND_EXPECTED,        handler: &NonImplementedInstr, argDecoder: &HandleBUncond),
            new(mask: BL_BLX_PREFIX_MASK,   expected: BL_BLX_PREFIX_EXPECTED,   handler: &NonImplementedInstr, argDecoder: &HandleBlBlxPrefix),
            new(mask: BL_SUFFIX_MASK,       expected: BL_SUFFIX_EXPECTED,       handler: &NonImplementedInstr, argDecoder: &HandleBlSuffix),
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
                    if ((instr << 8 & pattern.Mask) == pattern.Expected)
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