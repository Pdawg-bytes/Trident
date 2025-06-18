using Trident.Core.Enums;

namespace Trident.Core.CPU
{
    public struct Pipeline
    {
        public uint[] Prefetch;
        public PipelineAccess Access;

        public Pipeline()
        {
            Prefetch = new uint[2];
        }
    }
}