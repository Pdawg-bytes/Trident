using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.GamePak.Backup;

internal sealed class Flash : IBackupDevice
{
    private const uint CmdAddr1    = 0x0E005555;
    private const uint CmdAddr2    = 0x0E002AAA;
    private const uint CmdAddrBase = 0x0E000000;
    private const byte CmdByte1    = 0xAA;
    private const byte CmdByte2    = 0x55;
    private const uint BankSize    = 65536;
    private const uint SectorSize  = 0x1000;
    private const uint SectorMask  = 0xF000;
    private const uint AddressMask = 0xFFFF;
    private const byte ErasedByte  = 0xFF;

    private readonly UnsafeMemoryBlock _memory;
    private readonly uint _memorySize;
    private readonly bool _is128K;
    
    private byte _phase;
    private byte _currentBank;
    private bool _chipIDMode;
    private bool _eraseMode;
    private bool _writeMode;
    private bool _selectMode;

    public BackupType Type { get; }
    public uint Size => _memorySize;

    public Flash(BackupType type, byte[]? existingSaveData = null)
    {
        Type        = type;
        _is128K     = type == BackupType.Flash128K;
        _memorySize = _is128K ? 128 * 1024u : 64 * 1024u;
        
        _memory = new(_memorySize);
        
        if (existingSaveData != null && existingSaveData.Length == _memorySize)
            _memory.WriteBytes(0, existingSaveData);
        else
            _memory.Clear(ErasedByte);

        Reset();
    }


    public byte Read(uint address)
    {
        address &= AddressMask;
        
        if (_chipIDMode && address < 2)
        {
            // Macronix 128K: 09C2h, SST 64K: D4BFh
            return _is128K
                ? (address == 0 ? (byte)0xC2 : (byte)0x09)
                : (address == 0 ? (byte)0xBF : (byte)0xD4);
        }
        
        uint bankOffset = _is128K ? (uint)_currentBank * BankSize : 0;
        return _memory.Read<byte>((address & AddressMask) + bankOffset);
    }

    public void Write(uint address, byte value)
    {
        switch (_phase)
        {
            case 0:
                if (address == CmdAddr1 && value == CmdByte1)
                    _phase = 1;
                break;
            
            case 1:
                _phase = (byte)((address == CmdAddr2 && value == CmdByte2) ? 2 : 0);
                break;
            
            case 2:
                if (address == CmdAddr1)
                {
                    HandleStandardCommand((Command)value);
                }
                else if (_eraseMode && (address & ~SectorMask) == CmdAddrBase && (Command)value == Command.EraseSector)
                {
                    uint physicalBase = GetPhysicalAddress(address & SectorMask);

                    for (uint i = 0; i < SectorSize; i++)
                        _memory.Write(physicalBase + i, ErasedByte);
                    
                    _eraseMode = false;
                    _phase = 0;
                }
                else
                {
                    _phase = 0;
                }
                break;
            
            case 3:
                if (_writeMode)
                {
                    uint physicalAddr = GetPhysicalAddress(address & AddressMask);
                    byte current      = _memory.Read<byte>(physicalAddr);
                    _memory.Write(physicalAddr, (byte)(current & value));
                    _writeMode = false;
                }
                else if (_selectMode && address == CmdAddrBase)
                {
                    _currentBank = (byte)(value & 1);
                    _selectMode  = false;
                }
                _phase = 0;
                break;
        }
    }

    private void HandleStandardCommand(Command command)
    {
        switch (command)
        {
            case Command.ReadChipID:
                _chipIDMode = true;
                break;

            case Command.FinishChipID:
                _chipIDMode = false;
                break;

            case Command.Erase:
                _eraseMode = true;
                break;

            case Command.EraseChip:
                if (_eraseMode)
                {
                    uint size  = _is128K ? BankSize : _memorySize;
                    uint start = _is128K ? (uint)_currentBank * BankSize : 0;

                    for (uint i = 0; i < size; i++)
                        _memory.Write(start + i, ErasedByte);

                    _eraseMode = false;
                }
                break;

            case Command.WriteByte:
                _writeMode = true;
                _phase     = 3;
                return;

            case Command.SelectBank:
                if (_is128K)
                {
                    _selectMode = true;
                    _phase      = 3;
                    return;
                }
                break;
        }

        _phase = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetPhysicalAddress(uint address)
    {
        uint bankOffset = _is128K ? (uint)_currentBank * BankSize : 0;
        return (address & AddressMask) + bankOffset;
    }


    public void Reset()
    {
        _phase       = 0;
        _currentBank = 0;
        _chipIDMode  = false;
        _eraseMode   = false;
        _writeMode   = false;
        _selectMode  = false;
    }


    public byte[] GetSaveData()
    {
        byte[] data = new byte[_memorySize];

        for (uint i = 0; i < _memorySize; i++)
            data[i] = _memory.Read<byte>(i);

        return data;
    }

    public void LoadSaveData(byte[] data)
    {
        if (data.Length != _memorySize)
            throw new ArgumentException($"Flash save data must be exactly {_memorySize} bytes");
        
        _memory.WriteBytes(0, data);
    }


    public void Dispose() => _memory.Dispose();


    private enum Command : byte
    {
        ReadChipID   = 0x90,
        FinishChipID = 0xF0,
        Erase        = 0x80,
        EraseChip    = 0x10,
        EraseSector  = 0x30,
        WriteByte    = 0xA0,
        SelectBank   = 0xB0
    }
}