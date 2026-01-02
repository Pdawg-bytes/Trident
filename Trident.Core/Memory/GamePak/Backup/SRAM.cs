using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.GamePak.Backup;

internal class SRAM : IBackupDevice
{
    public uint Size => MemorySize;
    public BackupType Type => BackupType.SRAM;

    internal const uint MemorySize = 32 * 1024;
    private const uint AddressMask = MemorySize - 1;
    private readonly UnsafeMemoryBlock _memory = new(MemorySize);

    internal SRAM(byte[]? saveData)
    {
        if (saveData != null)
            _memory.WriteBytes(0, saveData);
    }

    public void Reset() => _memory.Clear();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read(uint address) => _memory.Read8(address & AddressMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint address, byte value) => _memory.Write8(address & AddressMask, value);

    public void Dispose() => _memory.Dispose();
}