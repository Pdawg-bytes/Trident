namespace Trident.CodeGeneration.Shared;

public enum ThumbGroup
{
    ShiftImmediate,
    AddSubtract,
    ImmediateOperations,
    ThumbALU,
    HiRegisterOps,
    BranchExchange,
    LoadPCRelative,
    LoadStoreRegOffset,
    LoadStoreSigned,
    LoadStoreImmOffset,
    LoadStore16,
    LoadStoreSPRelative,
    LoadAddress,
    AddOffsetSP,
    PushPop,
    LoadStoreMultiple,
    ConditionalBranch,
    SoftwareInterrupt,
    UnconditionalBranch,
    LongBranchWithLink,
    Undefined
}