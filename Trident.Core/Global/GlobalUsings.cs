global using unsafe ARMInstruction = delegate* managed<Trident.Core.CPU.ARM7TDMI, uint, uint>;

global using unsafe ThumbInstruction = delegate* managed<Trident.Core.CPU.ARM7TDMI, ref Trident.Core.CPU.Decoding.Thumb.ThumbArguments, uint>;
global using unsafe ThumbArgumentDecoder = delegate* managed<ref Trident.Core.CPU.Decoding.Thumb.ThumbArguments, uint, void>;