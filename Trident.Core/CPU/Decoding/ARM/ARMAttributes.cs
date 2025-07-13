using Trident.CodeGeneration.Shared;

namespace Trident.Core.CPU.Decoding.ARM
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ARMParameterAttribute<T> : Attribute
    {
        public ARMParameterAttribute(string name, int size = -1, int bit = -1, int hi = -1, int lo = -1)
        {
            Name = name; Size = size; Bit = bit; High = hi; Low = lo;
        }

        public string Name { get; }
        public int Size { get; }
        public int Bit { get; }
        public int High { get; }
        public int Low { get; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ARMGroupAttribute : Attribute
    {
        public ARMGroupAttribute(ARMGroup group)
        {
            Group = (int)group;
        }

        public int Group { get; }
    }
}