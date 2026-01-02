namespace Trident.Core.CPU.Pipeline;

public struct InstructionPipeline
{
    public uint[] Prefetch;
    public PipelineAccess Access;

    public InstructionPipeline() => Prefetch = new uint[2];
}