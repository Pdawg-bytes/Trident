global using ARMInstruction = ARMInstructionDelegate;
global using ThumbInstruction = ThumbInstructionDelegate;

internal delegate void ARMInstructionDelegate(uint opcode);
internal delegate void ThumbInstructionDelegate(ushort opcode);