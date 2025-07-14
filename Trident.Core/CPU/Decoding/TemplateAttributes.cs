namespace Trident.Core.CPU.Decoding
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TemplateParameterAttribute<T>(string name, int size = -1, int bit = -1, int hi = -1, int lo = -1) : Attribute
    {
        public string Name { get; } = name;
        public int Size { get; } = size;
        public int Bit { get; } = bit;
        public int High { get; } = hi;
        public int Low { get; } = lo;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TemplateGroupAttribute<T>(T group) : Attribute
        where T : Enum
    {
        public int Group { get; } = Convert.ToInt32(group);
    }
}