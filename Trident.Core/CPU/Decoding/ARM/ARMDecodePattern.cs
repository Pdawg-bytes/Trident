namespace Trident.Core.CPU.Decoding.ARM
{
    readonly unsafe struct ARMDecodePattern
    {
        internal readonly ARMMask Mask;
        internal readonly ARMExpected Expected;
        internal readonly ARMInstruction Handler;

        internal ARMDecodePattern(ARMMask mask, ARMExpected expected, ARMInstruction handler)
        {
            Mask = mask;
            Expected = expected;
            Handler = handler;
        }
    }

    internal enum ARMMask : uint
    {
        MUL = 0b111111001111,
        MULL = 0b111110001111,
        SWP = 0b111110111111,
        LDRH_STRH = 0b111000001111,
        LDRSB_LDRSH = 0b111000011101,
        MRS = 0b111110111111,
        MSR_REG = 0b111110111111,
        MSR_IMM = 0b111110110000,
        BX = 0b111111111111,
        DP_IMM_SHIFT = 0b111000000001,
        DP_REG_SHIFT = 0b111000001001,
        UNDEFINED = 0b111110110000,
        DP_IMM = 0b111000000000,
        LDR_STR_IMM = 0b111000000000,
        LDR_STR_REG = 0b111000000001,
        LDM_STM = 0b111000000000,
        B_BL = 0b111000000000,
        STC_LDC = 0b111000000000,
        CDP = 0b111100000001,
        MCR_MRC = 0b111100000001,
        SWI = 0b111100000000,
    }

    internal enum ARMExpected : uint
    {
        MUL = 0b000000001001,
        MULL = 0b000010001001,
        SWP = 0b000100001001,
        LDRH_STRH = 0b000000001011,
        LDRSB_LDRSH = 0b000000011101,
        MRS = 0b000100000000,
        MSR_REG = 0b000100100000,
        MSR_IMM = 0b001100100000,
        BX = 0b000100100001,
        DP_IMM_SHIFT = 0b000000000000,
        DP_REG_SHIFT = 0b000000000001,
        UNDEFINED = 0b001100000000,
        DP_IMM = 0b001000000000,
        LDR_STR_IMM = 0b010000000000,
        LDR_STR_REG = 0b011000000000,
        LDM_STM = 0b100000000000,
        B_BL = 0b101000000000,
        STC_LDC = 0b110000000000,
        CDP = 0b111000000000,
        MCR_MRC = 0b111000000001,
        SWI = 0b111100000000,
    }
}