using Trident.Core.Enums;

namespace Trident.Core.CPU
{
    internal struct Pipeline
    {
        internal uint[] Prefetch;
        internal uint[] Address;
        internal PipelineAccess Access;

        public Pipeline()
        {
            Prefetch = new uint[2];
            Address = new uint[2];
        }
    }
}