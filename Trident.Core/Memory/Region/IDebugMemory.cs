namespace Trident.Core.Memory.Region
{
    public interface IDebugMemory
    {
        T DebugRead<T>(uint address) where T : unmanaged;


        uint BaseAddress { get; }
        uint Length { get; }
        uint EndAddress => BaseAddress + Length;
    }
}