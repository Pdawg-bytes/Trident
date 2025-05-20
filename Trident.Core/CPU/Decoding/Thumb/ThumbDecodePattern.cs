namespace Trident.Core.CPU.Decoding.Thumb
{
    readonly unsafe struct ThumbDecodePattern
    {
        internal readonly ThumbMask Mask;
        internal readonly ThumbExpected Expected;
        internal readonly ThumbInstruction Handler;
        internal readonly ThumbArgumentDecoder ParamDecoder;

        internal ThumbDecodePattern(ThumbMask mask, ThumbExpected expected, ThumbInstruction handler, ThumbArgumentDecoder argDecoder)
        {
            Mask = mask;
            Expected = expected;
            Handler = handler;
            ParamDecoder = argDecoder;
        }
    }

    internal enum ThumbMask : ushort
    {
        ADD_SUB = 0b1111100000000000,
        SHIFT_IMM = 0b1110000000000000,
        MOV_CMP_ADD_SUB = 0b1110000000000000,
        DP_REG = 0b1111110000000000,
        BX = 0b1111111100000000,
        HIGH_REG_OPS = 0b1111110000000000,
        LDR_PC_REL = 0b1111100000000000,
        LDRH_STRH_REG = 0b1111011000000000,
        LDRSB_LDRSH_REG = 0b1111011000000000,
        LDR_STR_REG = 0b1111011000000000,
        LDRB_STRB_REG = 0b1111011000000000,
        LDR_STR_IMM = 0b1111000000000000,
        LDRB_STRB_IMM = 0b1111000000000000,
        LDRH_STRH_IMM = 0b1111000000000000,
        LDR_STR_SP_REL = 0b1111000000000000,
        ADD_SP_PC = 0b1111000000000000,
        ADD_SUB_SP = 0b1111110000000000,
        PUSH_POP = 0b1111011000000000,
        LDM_STM = 0b1111000000000000,
        SWI = 0b1111111100000000,
        UNDEFINED_BCC = 0b1111111100000000,
        BCC = 0b1111000000000000,
        B_UNCOND = 0b1111100000000000,
        BL_BLX_PREFIX = 0b1111100000000000,
        BL_SUFFIX = 0b1111100000000000,
    }

    internal enum ThumbExpected : ushort
    {
        ADD_SUB = 0b0001100000000000,
        SHIFT_IMM = 0b0000000000000000,
        MOV_CMP_ADD_SUB = 0b0010000000000000,
        DP_REG = 0b0100000000000000,
        BX = 0b0100011100000000,
        HIGH_REG_OPS = 0b0100010000000000,
        LDR_PC_REL = 0b0100100000000000,
        LDRH_STRH_REG = 0b0101001000000000,
        LDRSB_LDRSH_REG = 0b0101011000000000,
        LDR_STR_REG = 0b0101000000000000,
        LDRB_STRB_REG = 0b0101010000000000,
        LDR_STR_IMM = 0b0110000000000000,
        LDRB_STRB_IMM = 0b0111000000000000,
        LDRH_STRH_IMM = 0b1000000000000000,
        LDR_STR_SP_REL = 0b1001000000000000,
        ADD_SP_PC = 0b1010000000000000,
        ADD_SUB_SP = 0b1011000000000000,
        PUSH_POP = 0b1011010000000000,
        LDM_STM = 0b1100000000000000,
        SWI = 0b1101111100000000,
        UNDEFINED_BCC = 0b1101111000000000,
        BCC = 0b1101000000000000,
        B_UNCOND = 0b1110000000000000,
        BL_BLX_PREFIX = 0b1111000000000000,
        BL_SUFFIX = 0b1111100000000000,
    }

}