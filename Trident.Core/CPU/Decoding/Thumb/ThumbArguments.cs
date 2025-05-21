namespace Trident.Core.CPU.Decoding.Thumb
{
    internal struct ThumbArguments
    {
        internal uint SubOpcode;
        internal uint Opcode;
        internal uint Rd, Rs;
        internal uint Ro, Rm;
        internal uint Rn, Rb;

        internal uint L, I, B, SP, S, PC_LR;
        internal uint Imm;
        internal uint Rlist;
        internal uint Comment;

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