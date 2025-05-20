namespace Trident.Core.CPU.Decoding.Thumb
{
    readonly unsafe struct ThumbDecodePattern
    {
        internal readonly uint Mask;
        internal readonly uint Expected;
        internal readonly ThumbInstruction Handler;
        internal readonly ThumbArgumentDecoder ParamDecoder;

        internal ThumbDecodePattern(uint mask, uint expected, ThumbInstruction handler, ThumbArgumentDecoder paramDecoder)
        {
            Mask = mask;
            Expected = expected;
            Handler = handler;
            ParamDecoder = paramDecoder;
        }
    }
}