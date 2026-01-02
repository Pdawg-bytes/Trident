using Trident.Core.Bus;
using Trident.Core.Global;
using Trident.Core.CPU.Pipeline;
using Trident.Core.CPU.Decoding;
using Trident.Core.CPU.Registers;
using Trident.CodeGeneration.Shared;
using Trident.Core.CPU.Decoding.ARM;

namespace Trident.Core.CPU;

public partial class ARM7TDMI<TBus> where TBus : struct, IDataBus
{
    [TemplateGroup<ARMGroup>(ARMGroup.CoprocDataOperation)]
    internal void CoprocDataOperation(uint opcode)
    {
        // This is currently a stub.
        ARM_Undefined(opcode);
    }


    [TemplateGroup<ARMGroup>(ARMGroup.CoprocDataTransfer)]
    internal void CoprocDataTransfer(uint opcode)
    {
        // This is currently a stub.
        ARM_Undefined(opcode);
    }


    [TemplateGroup<ARMGroup>(ARMGroup.CoprocRegisterTransfer)]
    internal void CoprocRegisterTransfer(uint opcode)
    {
        // This is currently a stub.
        ARM_Undefined(opcode);
    }
}