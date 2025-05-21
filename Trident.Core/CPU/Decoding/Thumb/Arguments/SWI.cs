using Trident.Core.Global;

namespace Trident.Core.CPU.Decoding.Thumb
{
    internal unsafe static partial class ThumbArgumentDecoders
    {
        internal static void HandleSWI(ref ThumbArguments args, uint opcode)
        {
            args.Opcode = opcode;
            args.Comment = opcode.Extract(7, 0);
        }
    }
}