namespace Trident.Core.Memory.GamePak.Backup;

internal sealed class SRAM : IBackupDevice
{
    private const uint MemorySize = 32 * 1024;
    private readonly UnsafeMemoryBlock _memory;

    public BackupType Type => BackupType.SRAM;
    public uint Size       => MemorySize;

    public SRAM(byte[]? existingSaveData = null)
    {
        _memory = new(MemorySize);

        if (existingSaveData != null && existingSaveData.Length == MemorySize)
            _memory.WriteBytes(0, existingSaveData);
        else
            _memory.Clear(0xFF);
    }


    public byte Read(uint address)
    {
        address &= 0x7FFF;
        return _memory.Read<byte>(address);
    }

    public void Write(uint address, byte value)
    {
        address &= 0x7FFF;
        _memory.Write(address, value);
    }


    public void Reset() { }


    public byte[] GetSaveData()
    {
        byte[] data = new byte[MemorySize];

        for (uint i = 0; i < MemorySize; i++)
            data[i] = _memory.Read<byte>(i);

        return data;
    }

    public void LoadSaveData(byte[] data)
    {
        if (data.Length != MemorySize)
            throw new ArgumentException($"SRAM save data must be exactly {MemorySize} bytes");

        _memory.WriteBytes(0, data);
    }


    public void Dispose() => _memory.Dispose();
}