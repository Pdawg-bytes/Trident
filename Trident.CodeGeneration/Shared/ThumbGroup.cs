namespace Trident.CodeGeneration.Shared
{
    public enum ThumbGroup
    {
        ShiftImmediate,
        AddSubtract,
        UnnamedGroup3,
        ThumbALU,
        HiRegisterOps,
        BranchExchange,
        LoadStorePCRelative,
        LoadStoreSigned,
        LoadStoreImmOffset,
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
}