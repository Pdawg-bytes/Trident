namespace Trident.Core.CPU.Decoding.Thumb
{
    internal unsafe struct ThumbMetadata
    {
        internal ThumbInstruction Handler;
        internal ThumbArgumentDecoder ArgDecoder;
    }
}