namespace Trident.Core.CPU.Pipeline
{
    [Flags]
    public enum PipelineAccess : uint
    {
        NonSequential = 0,
        Sequential    = 1 << 0,
        Code          = 1 << 1,
        DMA           = 1 << 2,
        Lock          = 1 << 4
    }
}