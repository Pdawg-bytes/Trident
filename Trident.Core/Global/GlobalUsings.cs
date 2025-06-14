global using ARMInstruction = ARMInstructionDelegate;
global using ThumbInstruction = ThumbInstructionDelegate;
global using ThumbArgumentDecoder = ThumbArgumentDecoderDelegate;

internal delegate uint ARMInstructionDelegate(uint opcode);
internal delegate uint ThumbInstructionDelegate(ref Trident.Core.CPU.Decoding.Thumb.ThumbArguments args);
internal delegate void ThumbArgumentDecoderDelegate(ref Trident.Core.CPU.Decoding.Thumb.ThumbArguments args, uint instruction);