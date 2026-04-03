using Trident.Core.Global;

namespace Trident.Core.Memory.GamePak.Backup;

internal sealed class EEPROM : IBackupDevice
{
    private const int DataSizeBits = 64;
    private const uint AddressMask = 0x1FFF;
    
    public BackupType Type { get; }
    public uint Size => _memorySize;

    private readonly UnsafeMemoryBlock _memory;
    private readonly uint _memorySize;
    private readonly uint _addressBits;
    
    private ulong _buffer;
    private int _bitCount;
    private uint _address;
    private EEPROMState _state;

    public EEPROM(BackupType type, byte[]? existingSaveData = null)
    {
        Type         = type;
        _memorySize  = type == BackupType.EEPROM512B ? 512u : 8192u;
        _addressBits = type == BackupType.EEPROM512B ? 6u   : 14u;
        
        _memory = new(_memorySize);
        
        if (existingSaveData != null && existingSaveData.Length == _memorySize)
            _memory.WriteBytes(0, existingSaveData);
        else
            _memory.Clear(0xFF);
        
        Reset();
    }


    public byte Read(uint address)
    {
        if (!_state.Has(EEPROMState.Reading))
            return (byte)(_state.Has(EEPROMState.Busy) ? 0 : 1);

        if (_state.Has(EEPROMState.DummyNibble))
        {
            if (++_bitCount == 4)
            {
                _state &= ~EEPROMState.DummyNibble;
                ResetBuffer();
            }

            return 0;
        }
        
        int bitIndex    = _bitCount & 7;
        int byteIndex   = _bitCount >> 3;
        byte memoryByte = _memory.Read<byte>(_address + (uint)byteIndex);
        
        if (++_bitCount == DataSizeBits)
        {
            _state = EEPROMState.AcceptCommand;
            ResetBuffer();
        }
        
        return (byte)((memoryByte >> (7 - bitIndex)) & 1);
    }

    public void Write(uint address, byte value)
    {
        if (_state.Has(EEPROMState.Reading) || _state.Has(EEPROMState.Busy))
            return;

        _buffer = (_buffer << 1) | ((value & 1) != 0 ? 1UL : 0UL);
        _bitCount++;

        if (_state == EEPROMState.AcceptCommand && _bitCount == 2)
        {
            _state = _buffer switch
            {
                0b10 => EEPROMState.WriteMode | EEPROMState.GetAddress | EEPROMState.Writing | EEPROMState.SkipDummy,
                0b11 => EEPROMState.ReadMode  | EEPROMState.GetAddress | EEPROMState.SkipDummy,
                _    => _state
            };

            ResetBuffer();
        }
        else if (_state.Has(EEPROMState.GetAddress) && _bitCount == _addressBits)
        {
            _address = (uint)(_buffer & ((1u << (int)_addressBits) - 1)) << 3;
            _address &= AddressMask;

            if (_state.Has(EEPROMState.WriteMode))
            {
                for (uint i = 0; i < 8; i++)
                    _memory.Write(_address + i, (byte)0);
            }

            _state &= ~EEPROMState.GetAddress;
            ResetBuffer();
        }
        else if (_state.Has(EEPROMState.Writing))
        {
            int bitIndex  = (_bitCount - 1) & 7;
            int byteIndex = (_bitCount - 1) >> 3;

            byte current = _memory.Read<byte>(_address + (uint)byteIndex);
            _memory.Write(_address + (uint)byteIndex, (byte)(current | ((value & 1) << (7 - bitIndex))));
            
            if (_bitCount == DataSizeBits)
            {
                _state &= ~EEPROMState.Writing;
                ResetBuffer();
            }
        }
        else if (_state.Has(EEPROMState.SkipDummy))
        {
            _state &= ~EEPROMState.SkipDummy;

            if (_state.Has(EEPROMState.ReadMode))       _state |= EEPROMState.Reading | EEPROMState.DummyNibble;
            else if (_state.Has(EEPROMState.WriteMode)) _state  = EEPROMState.AcceptCommand;

            ResetBuffer();
        }
    }

    private void ResetBuffer()
    {
        _buffer = 0;
        _bitCount = 0;
    }


    public void Reset()
    {
        _state    = EEPROMState.AcceptCommand;
        _address  = 0;
        _buffer   = 0;
        _bitCount = 0;
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
            throw new ArgumentException($"EEPROM save data must be exactly {_memorySize} bytes");
        
        _memory.WriteBytes(0, data);
    }


    public void Dispose() => _memory.Dispose();


    [Flags]
    private enum EEPROMState : byte
    {
        AcceptCommand = 0,
        DummyNibble   = 1 << 0,
        Reading       = 1 << 1,
        Writing       = 1 << 2,
        GetAddress    = 1 << 3,
        ReadMode      = 1 << 4,
        WriteMode     = 1 << 5,
        SkipDummy     = 1 << 6,
        Busy          = 1 << 7
    }
}