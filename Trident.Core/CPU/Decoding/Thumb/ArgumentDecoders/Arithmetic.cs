using Trident.Core.Global;

namespace Trident.Core.CPU.Decoding.Thumb
{
    internal unsafe static partial class ThumbArgumentDecoders
    {
        internal static void HandleAddSub(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.SubOp = opcode.GetBit(9);

            args.I = opcode.GetBit(10);
            if (args.I == 0)
                args.Rn = opcode.Extract(8, 6);
            else
                args.Imm = opcode.Extract(8, 6);

            args.Rs = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal static void HandleShiftImm(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.SubOp = opcode.Extract(12, 11);

            args.Imm = opcode.Extract(10, 6);
            args.Rs = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }

        internal static void HandleMovCmpAddSub(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.SubOp = opcode.Extract(12, 11);

            args.Rd = opcode.Extract(10, 8);
            args.Imm = opcode.Extract(7, 0);
        }

        internal static void HandleDataProcReg(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.SubOp = opcode.Extract(9, 6);

            args.Rs = opcode.Extract(5, 3);
            args.Rd = opcode.Extract(2, 0);
        }
    }
}