namespace Trident.Core.Memory.GamePak;

internal interface IAccess
{
    static abstract bool IsLower { get; }
}

internal struct LowerAccess : IAccess
{
    public static bool IsLower => true;
}

internal struct UpperAccess : IAccess
{
    public static bool IsLower => false;
}