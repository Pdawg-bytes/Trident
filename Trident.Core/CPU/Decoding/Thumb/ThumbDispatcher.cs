using static Trident.Core.CPU.Decoding.Thumb.ThumbArgumentHandler;

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
            new(mask: 0b1111100000000000, expected: 0b0001100000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // ADD, SUB
            new(mask: 0b1110000000000000, expected: 0b0000000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LSL, LSR, ASR, ROR
            new(mask: 0b1110000000000000, expected: 0b0010000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // MOV, CMP, ADD, SUB
            new(mask: 0b1111110000000000, expected: 0b0100000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // Data Processing
            new(mask: 0b1111111100000000, expected: 0b0100011100000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // BX
            new(mask: 0b1111110000000000, expected: 0b0100010000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // ADD, CMP, MOV (high registers)
            new(mask: 0b1111100000000000, expected: 0b0100100000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LDR (PC-relative)
            new(mask: 0b1111011000000000, expected: 0b0101001000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LDRH, STRH (register offset)
            new(mask: 0b1111011000000000, expected: 0b0101011000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LDRSH, LDRSB (register offset)
            new(mask: 0b1111011000000000, expected: 0b0101000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LDR, STR (register offset)
            new(mask: 0b1111011000000000, expected: 0b0101010000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LDRB, STRB (register offset)
            new(mask: 0b1111000000000000, expected: 0b0110000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LDR, STR (immediate offset)
            new(mask: 0b1111000000000000, expected: 0b0111000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LDRB, STRB (immediate offset)
            new(mask: 0b1111000000000000, expected: 0b1000000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LDRH, STRH (immediate offset)
            new(mask: 0b1111000000000000, expected: 0b1001000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LDR, STR (SP-relative)
            new(mask: 0b1111000000000000, expected: 0b1010000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // ADD (SP or PC) aka Load Address
            new(mask: 0b1111110000000000, expected: 0b1011000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // ADD, SUB (SP)
            new(mask: 0b1111011000000000, expected: 0b1011010000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // PUSH, POP
            new(mask: 0b1111000000000000, expected: 0b1100000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // LDM, STM
            new(mask: 0b1111111100000000, expected: 0b1101111100000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // SWI
            new(mask: 0b1111111100000000, expected: 0b1101111000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // Undefined instructions in Bcc range
            new(mask: 0b1111000000000000, expected: 0b1101000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // Bcc (conditional branching)
            new(mask: 0b1111100000000000, expected: 0b1110000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // B (unconditional branching)
            new(mask: 0b1111100000000000, expected: 0b1111000000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // BL, BLX prefix
            new(mask: 0b1111100000000000, expected: 0b1111100000000000, handler: &NotImplementedInstruction, &NotImplementedParamDecoder), // BL suffix
        ];


        /// <summary>
        /// Initializes the Thumb decoder table based on the instructions' 8-bit format.
        /// </summary>
        internal static void InitDecoder()
        {
            for (uint instr = 0; instr < THUMB_DISPATCH_COUNT; instr++)
            {
                _thumbInstructionTable[instr] = &NotImplementedInstruction; // Default

                foreach (var pattern in _instructionPatterns)
                {
                    if ((instr & pattern.Mask) == pattern.Expected)
                    {
                        _thumbInstructionTable[instr] = pattern.Handler;
                        _thumbArgDecoders[instr] = pattern.ParamDecoder;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the handler and parameter decoder for the current Thumb instruction.
        /// </summary>
        /// <param name="opcode">The Thumb instruction to return the handler of.</param>
        /// <returns>A delegate that points to the handler of the instruction, and its respective parameter decoder.</returns>
        internal static ref ThumbMetadata GetInstruction(uint opcode)
        {
            uint index = (opcode & 0xFF00) >> 8;

            _cachedThumbInstruction.Handler = _thumbInstructionTable[index];
            _cachedThumbInstruction.ArgDecoder = _thumbArgDecoders[index];

            return ref _cachedThumbInstruction;
        }

        private static uint NotImplementedInstruction(ARM7TDMI cpu, ref ThumbArguments args) => throw new NotImplementedException("This Thumb instruction group is not implemented.");
    }
}