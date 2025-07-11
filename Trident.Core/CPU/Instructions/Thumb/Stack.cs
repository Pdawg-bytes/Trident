using Trident.Core.Bus;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding.Thumb;

namespace Trident.Core.CPU
{
    public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
    {
        internal void Thumb_AddSubSP(ref ThumbArguments args)
        {
            Registers.SP += (args.S != 0) ? (uint)-args.Imm : args.Imm;

            Registers.PC += 2;
            Pipeline.Access = PipelineAccess.Sequential | PipelineAccess.Code;
        }
    }
}