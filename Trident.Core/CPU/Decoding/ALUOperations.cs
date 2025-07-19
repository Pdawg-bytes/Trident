namespace Trident.Core.CPU.Decoding
{
    internal enum ALUOpThumb : byte
    {
        AND = 0b0000,
        EOR = 0b0001,
        LSL = 0b0010,
        LSR = 0b0011,
        ASR = 0b0100,
        ADC = 0b0101,
        SBC = 0b0110,
        ROR = 0b0111,
        TST = 0b1000,
        NEG = 0b1001,
        CMP = 0b1010,
        CMN = 0b1011,
        ORR = 0b1100,
        MUL = 0b1101,
        BIC = 0b1110,
        MVN = 0b1111
    }

    internal enum ALUOpARM : byte
    {
        AND = 0b0000,
        EOR = 0b0001,
        SUB = 0b0010,
        RSB = 0b0011,
        ADD = 0b0100,
        ADC = 0b0101,
        SBC = 0b0110,
        RSC = 0b0111,
        TST = 0b1000,
        TEQ = 0b1001,
        CMP = 0b1010,
        CMN = 0b1011,
        ORR = 0b1100,
        MOV = 0b1101,
        BIC = 0b1110,
        MVN = 0b1111
    }
}