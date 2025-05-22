using Trident.Core.Global;

namespace Trident.Core.CPU.Decoding.Thumb
{
    internal unsafe static partial class ThumbArgumentDecoders
    {
        internal static void HandlePushPop(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.SubOp = opcode.GetBit(11);
            args.PC_LR = opcode.GetBit(8);
            args.Rlist = opcode.Extract(7, 0);
        }

        internal static void HandleLdmStm(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.SubOp = opcode.GetBit(11);
            args.Rb = opcode.Extract(10, 8);
            args.Rlist = opcode.Extract(7, 0);
        }
    }
}