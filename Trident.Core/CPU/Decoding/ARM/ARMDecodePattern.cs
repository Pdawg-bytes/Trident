namespace Trident.Core.CPU.Decoding.ARM
{
    readonly unsafe struct ARMDecodePattern
    {
        internal readonly uint Mask;
        internal readonly uint Expected;
        internal readonly ARMInstruction Handler;

        internal ARMDecodePattern(uint mask, uint expected, ARMInstruction handler)
        {
            Mask = mask;
            Expected = expected;
            Handler = handler;
        }
    }

    internal static class ARMDecodeMasks
    {
        internal const uint MUL_MASK = 0b111111001111;
        internal const uint MULL_MASK = 0b111110001111;
        internal const uint SWP_MASK = 0b111110111111;
        internal const uint LDRH_STRH_MASK = 0b111000001111;
        internal const uint LDRSB_LDRSH_MASK = 0b111000011101;
        internal const uint MRS_MASK = 0b111110111111;
        internal const uint MSR_REG_MASK = 0b111110111111;
        internal const uint MSR_IMM_MASK = 0b111110110000;
        internal const uint BX_MASK = 0b111111111111;
        internal const uint DP_IMM_SHIFT_MASK = 0b111000000001;
        internal const uint DP_REG_SHIFT_MASK = 0b111000001001;
        internal const uint UNDEFINED_MASK = 0b111110110000;
        internal const uint DP_IMM_MASK = 0b111000000000;
        internal const uint LDR_STR_IMM_MASK = 0b111000000000;
        internal const uint LDR_STR_REG_MASK = 0b111000000001;
        internal const uint LDM_STM_MASK = 0b111000000000;
        internal const uint B_BL_MASK = 0b111000000000;
        internal const uint STC_LDC_MASK = 0b111000000000;
        internal const uint CDP_MASK = 0b111100000001;
        internal const uint MCR_MRC_MASK = 0b111100000001;
        internal const uint SWI_MASK = 0b111100000000;

        internal const uint MUL_EXPECTED = 0b000000001001;
        internal const uint MULL_EXPECTED = 0b000010001001;
        internal const uint SWP_EXPECTED = 0b000100001001;
        internal const uint LDRH_STRH_EXPECTED = 0b000000001011;
        internal const uint LDRSB_LDRSH_EXPECTED = 0b000000011101;
        internal const uint MRS_EXPECTED = 0b000100000000;
        internal const uint MSR_REG_EXPECTED = 0b000100100000;
        internal const uint MSR_IMM_EXPECTED = 0b001100100000;
        internal const uint BX_EXPECTED = 0b000100100001;
        internal const uint DP_IMM_SHIFT_EXPECTED = 0b000000000000;
        internal const uint DP_REG_SHIFT_EXPECTED = 0b000000000001;
        internal const uint UNDEFINED_EXPECTED = 0b001100000000;
        internal const uint DP_IMM_EXPECTED = 0b001000000000;
        internal const uint LDR_STR_IMM_EXPECTED = 0b010000000000;
        internal const uint LDR_STR_REG_EXPECTED = 0b011000000000;
        internal const uint LDM_STM_EXPECTED = 0b100000000000;
        internal const uint B_BL_EXPECTED = 0b101000000000;
        internal const uint STC_LDC_EXPECTED = 0b110000000000;
        internal const uint CDP_EXPECTED = 0b111000000000;
        internal const uint MCR_MRC_EXPECTED = 0b111000000001;
        internal const uint SWI_EXPECTED = 0b111100000000;
    }
}