using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.Thumb;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        [TemplateParameter<bool>("Subtract", bit: 7)]
        [TemplateGroup<ThumbGroup>(ThumbGroup.AddOffsetSP)]
        internal void Thumb_AddOffsetSP<TTraits>(ushort opcode)
            where TTraits : struct, IThumb_AddOffsetSP_Traits
        {
            uint offset = ((uint)opcode & 0x7F) << 2;
            Registers.SP += TTraits.Subtract ? (uint)-offset : offset;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }
    }
}