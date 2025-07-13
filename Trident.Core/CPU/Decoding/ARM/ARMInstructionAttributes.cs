namespace Trident.Core.CPU.Decoding.ARM
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ARMParameterAttribute<T>(string name, int size = -1, int bit = -1, int hi = -1, int lo = -1) : Attribute
    {
        public string Name { get; } = name;
        public int Size { get; } = size;
        public int Bit { get; } = bit;
        public int High { get; } = hi; public int Low { get; } = lo;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ARMGroupAttribute(ARMGroup group) : Attribute
    {
        public int Group { get; } = (int)group;
    }

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
}