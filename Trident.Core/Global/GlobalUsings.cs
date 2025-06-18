global using ARMInstruction = ARMInstructionDelegate;
global using ThumbInstruction = ThumbInstructionDelegate;
global using ThumbArgumentDecoder = ThumbArgumentDecoderDelegate;

internal delegate void ARMInstructionDelegate(uint opcode);
internal delegate void ThumbInstructionDelegate(ref Trident.Core.CPU.Decoding.Thumb.ThumbArguments args);
internal delegate void ThumbArgumentDecoderDelegate(ref Trident.Core.CPU.Decoding.Thumb.ThumbArguments args, uint instruction);