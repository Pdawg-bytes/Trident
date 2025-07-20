using System.Numerics;
using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Pipeline;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.ARM;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateParameter<bool>("PreIndexed", bit: 24)]
        [TemplateParameter<bool>("AddOffset", bit: 23)]
        [TemplateParameter<bool>("UserMode", bit: 22)]
        [TemplateParameter<bool>("Writeback", bit: 21)]
        [TemplateParameter<bool>("Load", bit: 20)]
        [TemplateGroup<ARMGroup>(ARMGroup.BlockDataTransfer)]
        internal void ARM_BlockDataTransfer<TTraits>(uint opcode)
            where TTraits : struct, IARM_BlockDataTransfer_Traits
        {

        }
    }
}