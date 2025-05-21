using Trident.Core.Global;

namespace Trident.Core.CPU.Decoding.Thumb
{
    internal unsafe static partial class ThumbArgumentDecoders
    {
        internal static void HandleBX(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;

            args.Rd = opcode.GetBit(7) << 3;
            args.Rd |= opcode.Extract(2, 0);
            args.Rs = opcode.GetBit(6) << 3;
            args.Rs |= opcode.Extract(5, 3);
        }

        internal static void HandleHighRegOps(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.SubOp = opcode.Extract(9, 8);

            args.Rd = opcode.GetBit(7) << 3;
            args.Rd |= opcode.Extract(2, 0);
            args.Rs = opcode.GetBit(6) << 3;
            args.Rs |= opcode.Extract(5, 3);
        }

        internal static void HandleBlSuffix(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.Imm = opcode.Extract(10, 0) << 1;
        }


        internal static void HandleBCC(ref ThumbArguments args, uint opcode) => HandleBranch(ref args, opcode, 8, 7, shift: 1, subOpBits: (11, 8));

        internal static void HandleBUncond(ref ThumbArguments args, uint opcode) => HandleBranch(ref args, opcode, 11, 10, shift: 1);

        internal static void HandleBlBlxPrefix(ref ThumbArguments args, uint opcode) => HandleBranch(ref args, opcode, 11, 10, shift: 12);

        private static void HandleBranch(ref ThumbArguments args, uint opcode, int extBits, int immBits, int shift, (int high, int low)? subOpBits = null)
        {
            args.Opcode = opcode;

            if (subOpBits is not null)
                args.SubOp = opcode.Extract(subOpBits.Value.high, subOpBits.Value.low);

            args.Imm = opcode.Extract(immBits, 0).Extend(extBits) << shift;
        }
    }
}