namespace Trident.CodeGeneration.Shared;

public enum ARMGroup
{
    Multiply,
    MultiplyLong,
    BranchExchange,
    BranchWithLink,
    Swap,
    SmallSignedTransfer,
    DataProcessing,
    PSRTransfer,
    SingleDataTrasnfer,
    BlockDataTransfer,
    Undefined,
    SoftwareInterrupt,
    CoprocDataOperation,
    CoprocDataTransfer,
    CoprocRegisterTransfer
}