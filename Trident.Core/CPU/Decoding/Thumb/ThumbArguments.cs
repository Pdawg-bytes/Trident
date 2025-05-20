namespace Trident.Core.CPU.Decoding.Thumb
{
    internal struct ThumbArguments
    {
        internal ushort SubOpcode;
        internal ushort Opcode;
        internal ushort Rd, Rs;
        internal ushort Ro, Rm;
        internal ushort Rn, Rb;

        internal ushort L, I, B, SP, S, PC_LR;
        internal uint Imm;
        internal ushort Rlist;
        internal ushort Comment;

        internal void Reset()
        {
            SubOpcode = Opcode = Rd = Rs = Ro = Rm = Rn = Rb = 0;
            L = I = B = SP = S = PC_LR = 0;
            Imm = 0;
            Rlist = 0;
            Comment = 0;
        }
    }
}