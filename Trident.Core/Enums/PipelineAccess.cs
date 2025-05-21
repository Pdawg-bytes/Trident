namespace Trident.Core.Enums
{
    [Flags]
    internal enum PipelineAccess : uint
    {
        NonSequential = 0,
        Sequential = 1,
        Code = 2,
        DMA = 4,
        Lock = 8
    }
}