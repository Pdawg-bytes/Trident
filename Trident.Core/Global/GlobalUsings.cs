//global using unsafe ARMInstruction = delegate* managed<Trident.Core.CPU.ARM7TDMI, uint, uint>;
//global using unsafe ThumbInstruction = delegate* managed<Trident.Core.CPU.ARM7TDMI, ref Trident.Core.CPU.Decoding.Thumb.ThumbArguments, uint>;
//global using unsafe ThumbArgumentDecoder = delegate* managed<ref Trident.Core.CPU.Decoding.Thumb.ThumbArguments, uint, void>;
global using ARMInstruction = ARMInstructionDelegate;
global using ThumbInstruction = ThumbInstructionDelegate;
global using ThumbArgumentDecoder = ThumbArgumentDecoderDelegate;

internal delegate uint ARMInstructionDelegate(Trident.Core.CPU.ARM7TDMI cpu, uint opcode);
internal delegate uint ThumbInstructionDelegate(Trident.Core.CPU.ARM7TDMI cpu, ref Trident.Core.CPU.Decoding.Thumb.ThumbArguments args);
internal delegate void ThumbArgumentDecoderDelegate(ref Trident.Core.CPU.Decoding.Thumb.ThumbArguments args, uint instruction);