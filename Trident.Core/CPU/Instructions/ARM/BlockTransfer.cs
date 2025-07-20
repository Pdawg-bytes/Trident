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
        [TemplateParameter<bool>("Load", bit: 11)]
        [TemplateGroup<ARMGroup>(ARMGroup.BlockDataTransfer)]
        internal void ARM_BlockDataTransfer<TTraits>(uint opcode)
            where TTraits : struct, IARM_BlockDataTransfer_Traits
        {

        }
    }
}