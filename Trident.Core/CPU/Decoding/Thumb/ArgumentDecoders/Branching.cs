using System.Runtime.CompilerServices;
using Trident.Core.Global;

namespace Trident.Core.CPU.Decoding.Thumb
{
    internal unsafe partial class ThumbArgumentDecoders
    {
        internal void HandleBX(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;

            args.Rd = opcode.GetBit(7) << 3;
            args.Rd |= opcode.Extract(2, 0);
            args.Rs = opcode.GetBit(6) << 3;
            args.Rs |= opcode.Extract(5, 3);
        }

        internal void HandleBlSuffix(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.Imm = opcode.Extract(10, 0) << 1;
        }


        internal void HandleBCC(ref ThumbArguments args, uint opcode) => HandleBranch(ref args, opcode, extBits: 8, immBits: 7, shift: 1, subOpBits: (11, 8));

        internal void HandleBUncond(ref ThumbArguments args, uint opcode) => HandleBranch(ref args, opcode, extBits: 11, immBits: 10, shift: 1);

        internal void HandleBlBlxPrefix(ref ThumbArguments args, uint opcode) => HandleBranch(ref args, opcode, extBits: 11, immBits: 10, shift: 12);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleBranch(ref ThumbArguments args, uint opcode, int extBits, int immBits, int shift, (int high, int low)? subOpBits = null)
        {
            args.Opcode = opcode;

            if (subOpBits is not null)
                args.SubOp = opcode.Extract(subOpBits.Value.high, subOpBits.Value.low);

            args.Imm = (uint)opcode.Extract(immBits, 0).Extend(extBits) << shift;
        }
    }
}