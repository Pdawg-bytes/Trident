using static Trident.Core.CPU.Decoding.Thumb.ThumbDecodeMasks;

namespace Trident.Core.CPU.Decoding.Thumb
{
    internal sealed class ThumbDispatcher
    {
        private const int THUMB_DISPATCH_COUNT = 0xFF;
        private ThumbInstruction[] _instructionHandlers = new ThumbInstruction[THUMB_DISPATCH_COUNT];
        private ThumbArgumentDecoder[] _argumentDecoders = new ThumbArgumentDecoder[THUMB_DISPATCH_COUNT];

        private readonly ThumbArgumentDecoders _argDecoderInstance;
        private ThumbDecodePattern[] _decodePatterns;

        private static ThumbMetadata _cachedInstruction;

        internal ThumbDispatcher()
        {
            _argDecoderInstance = new ThumbArgumentDecoders();

            InitPatterns();
            InitDecoder();
        }

        /// <summary>
        /// Gets the handler and argument decoder for the current Thumb instruction.
        /// </summary>
        /// <param name="opcode">The Thumb instruction to return the handler of.</param>
        /// <returns>A delegate that points to the handler of the instruction, and another point to its respective argument decoder.</returns>
        internal ref ThumbMetadata GetInstruction(uint opcode)
        {
            uint index = (opcode & 0xFF00) >> 8;

            _cachedInstruction.Handler = _instructionHandlers[index];
            _cachedInstruction.ArgDecoder = _argumentDecoders[index];

            return ref _cachedInstruction;
        }


        private void InitPatterns()
        {
            _decodePatterns =
            [
                new(mask: ADD_SUB_MASK,         expected: ADD_SUB_EXPECTED,         handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleAddSub),
                new(mask: SHIFT_IMM_MASK,       expected: SHIFT_IMM_EXPECTED,       handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleShiftImm),
                new(mask: MOV_CMP_ADD_SUB_MASK, expected: MOV_CMP_ADD_SUB_EXPECTED, handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleMovCmpAddSub),
                new(mask: DP_REG_MASK,          expected: DP_REG_EXPECTED,          handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleDataProcReg),
                new(mask: BX_MASK,              expected: BX_EXPECTED,              handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleBX),
                new(mask: HIGH_REG_OPS_MASK,    expected: HIGH_REG_OPS_EXPECTED,    handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleHighRegOps),
                new(mask: LDR_PC_REL_MASK,      expected: LDR_PC_REL_EXPECTED,      handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleLdrPCRel),
                new(mask: LDRH_STRH_REG_MASK,   expected: LDRH_STRH_REG_EXPECTED,   handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleLdrhStrhReg),
                new(mask: LDRSH_LDRSB_REG_MASK, expected: LDRSB_LDRSH_REG_EXPECTED, handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleLdrsbLdrshReg),
                new(mask: LDR_STR_REG_MASK,     expected: LDR_STR_REG_EXPECTED,     handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleLdrStrReg),
                new(mask: LDRB_STRB_REG_MASK,   expected: LDRB_STRB_REG_EXPECTED,   handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleLdrbStrbReg),
                new(mask: LDR_STR_IMM_MASK,     expected: LDR_STR_IMM_EXPECTED,     handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleLdrStrImm),
                new(mask: LDRB_STRB_IMM_MASK,   expected: LDRB_STRB_IMM_EXPECTED,   handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleLdrbStrbImm),
                new(mask: LDRH_STRH_IMM_MASK,   expected: LDRH_STRH_IMM_EXPECTED,   handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleLdrhStrhImm),
                new(mask: LDR_STR_SP_REL_MASK,  expected: LDR_STR_SP_REL_EXPECTED,  handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleLdrStrSPRel),
                new(mask: ADD_SP_PC_MASK,       expected: ADD_SP_PC_EXPECTED,       handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleAddSPPC),
                new(mask: ADD_SUB_SP_MASK,      expected: ADD_SUB_SP_EXPECTED,      handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleAddSubSP),
                new(mask: PUSH_POP_MASK,        expected: PUSH_POP_EXPECTED,        handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandlePushPop),
                new(mask: LDM_STM_MASK,         expected: LDM_STM_EXPECTED,         handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleLdmStm),
                new(mask: SWI_MASK,             expected: SWI_EXPECTED,             handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleSWI),
                new(mask: UNDEFINED_BCC_MASK,   expected: UNDEFINED_BCC_EXPECTED,   handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleBCC),
                new(mask: BCC_MASK,             expected: BCC_EXPECTED,             handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleBCC),
                new(mask: B_UNCOND_MASK,        expected: B_UNCOND_EXPECTED,        handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleBUncond),
                new(mask: BL_BLX_PREFIX_MASK,   expected: BL_BLX_PREFIX_EXPECTED,   handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleBlBlxPrefix),
                new(mask: BL_SUFFIX_MASK,       expected: BL_SUFFIX_EXPECTED,       handler: NonImplementedInstr, argDecoder: _argDecoderInstance.HandleBlSuffix),
            ];
        }

        private void InitDecoder()
        {
            for (uint instr = 0; instr < THUMB_DISPATCH_COUNT; instr++)
            {
                // Default
                _instructionHandlers[instr] = NonImplementedInstr;
                _argumentDecoders[instr] = NonImplementedArgHandler;

                foreach (var pattern in _decodePatterns)
                {
                    if ((instr << 8 & pattern.Mask) == pattern.Expected)
                    {
                        _instructionHandlers[instr] = pattern.Handler;
                        _argumentDecoders[instr] = pattern.ParamDecoder;
                        break;
                    }
                }
            }
        }


        private uint NonImplementedInstr(ARM7TDMI cpu, ref ThumbArguments args) => throw new NotImplementedException("This Thumb instruction group is not implemented.");
        private void NonImplementedArgHandler(ref ThumbArguments args, uint opcode) => throw new NotImplementedException("This Thumb instruction group does not have an associated argument decoder.");
    }
}