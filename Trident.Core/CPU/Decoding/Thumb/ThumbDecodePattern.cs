namespace Trident.Core.CPU.Decoding.Thumb
{
    readonly unsafe struct ThumbDecodePattern
    {
        internal readonly uint Mask;
        internal readonly uint Expected;
        internal readonly ThumbInstruction Handler;
        internal readonly ThumbArgumentDecoder ArgumentDecoder;

        internal ThumbDecodePattern(uint mask, uint expected, ThumbInstruction handler, ThumbArgumentDecoder argumentDecoder)
        {
            Mask = mask;
            Expected = expected;
            Handler = handler;
            ArgumentDecoder = argumentDecoder;
        }
    }

    internal static class ThumbDecodeMasks
    {
        internal const uint ADD_SUB_MASK = 0b1111100000000000;
        internal const uint SHIFT_IMM_MASK = 0b1110000000000000;
        internal const uint MOV_CMP_ADD_SUB_MASK = 0b1110000000000000;
        internal const uint DP_REG_MASK = 0b1111110000000000;
        internal const uint BX_MASK = 0b1111111100000000;
        internal const uint HIGH_REG_OPS_MASK = 0b1111110000000000;
        internal const uint LDR_PC_REL_MASK = 0b1111100000000000;
        internal const uint LDRH_STRH_REG_MASK = 0b1111011000000000;
        internal const uint LDRSH_LDRSB_REG_MASK = 0b1111011000000000;
        internal const uint LDR_STR_REG_MASK = 0b1111011000000000;
        internal const uint LDRB_STRB_REG_MASK = 0b1111011000000000;
        internal const uint LDR_STR_IMM_MASK = 0b1111000000000000;
        internal const uint LDRB_STRB_IMM_MASK = 0b1111000000000000;
        internal const uint LDRH_STRH_IMM_MASK = 0b1111000000000000;
        internal const uint LDR_STR_SP_REL_MASK = 0b1111000000000000;
        internal const uint ADD_SP_PC_MASK = 0b1111000000000000;
        internal const uint ADD_SUB_SP_MASK = 0b1111110000000000;
        internal const uint PUSH_POP_MASK = 0b1111011000000000;
        internal const uint LDM_STM_MASK = 0b1111000000000000;
        internal const uint SWI_MASK = 0b1111111100000000;
        internal const uint UNDEFINED_BCC_MASK = 0b1111111100000000;
        internal const uint BCC_MASK = 0b1111000000000000;
        internal const uint B_UNCOND_MASK = 0b1111100000000000;
        internal const uint BL_BLX_PREFIX_MASK = 0b1111100000000000;
        internal const uint BL_SUFFIX_MASK = 0b1111100000000000;

        internal const uint ADD_SUB_EXPECTED = 0b0001100000000000;
        internal const uint SHIFT_IMM_EXPECTED = 0b0000000000000000;
        internal const uint MOV_CMP_ADD_SUB_EXPECTED = 0b0010000000000000;
        internal const uint DP_REG_EXPECTED = 0b0100000000000000;
        internal const uint BX_EXPECTED = 0b0100011100000000;
        internal const uint HIGH_REG_OPS_EXPECTED = 0b0100010000000000;
        internal const uint LDR_PC_REL_EXPECTED = 0b0100100000000000;
        internal const uint LDRH_STRH_REG_EXPECTED = 0b0101001000000000;
        internal const uint LDRSB_LDRSH_REG_EXPECTED = 0b0101011000000000;
        internal const uint LDR_STR_REG_EXPECTED = 0b0101000000000000;
        internal const uint LDRB_STRB_REG_EXPECTED = 0b0101010000000000;
        internal const uint LDR_STR_IMM_EXPECTED = 0b0110000000000000;
        internal const uint LDRB_STRB_IMM_EXPECTED = 0b0111000000000000;
        internal const uint LDRH_STRH_IMM_EXPECTED = 0b1000000000000000;
        internal const uint LDR_STR_SP_REL_EXPECTED = 0b1001000000000000;
        internal const uint ADD_SP_PC_EXPECTED = 0b1010000000000000;
        internal const uint ADD_SUB_SP_EXPECTED = 0b1011000000000000;
        internal const uint PUSH_POP_EXPECTED = 0b1011010000000000;
        internal const uint LDM_STM_EXPECTED = 0b1100000000000000;
        internal const uint SWI_EXPECTED = 0b1101111100000000;
        internal const uint UNDEFINED_BCC_EXPECTED = 0b1101111000000000;
        internal const uint BCC_EXPECTED = 0b1101000000000000;
        internal const uint B_UNCOND_EXPECTED = 0b1110000000000000;
        internal const uint BL_BLX_PREFIX_EXPECTED = 0b1111000000000000;
        internal const uint BL_SUFFIX_EXPECTED = 0b1111100000000000;
    }
}