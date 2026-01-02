using Trident.Core.Hardware.IO;
using Trident.Core.Memory.Region;
using System.Runtime.CompilerServices;
using Trident.Core.Memory.GamePak.GPIO;
using Trident.Core.Memory.GamePak.Backup;

namespace Trident.Core.Memory.GamePak;

internal partial class GamePak : IDisposable, IDebugMemory
{
    internal const int MaxSize = 32 * 1024 * 1024;
    internal readonly int ActualSize;

    internal readonly GamePakInfo PakInfo;

    private UnsafeMemoryBlock _romMemory;
    private readonly uint _addressMask;
    private uint _romAddress;

    private readonly IBackupDevice _backupDevice;
    private readonly bool _isEEPROM;
    private readonly uint _eepromMask;

    private readonly GPIOBus _gpio;
    private readonly bool _isGPIO = false;

    private readonly Action<uint> _step;
    private readonly WaitControl _waitControl;

    private readonly IMemoryRegion _upperRegion;
    private readonly IMemoryRegion _lowerRegion;
    private readonly IMemoryRegion _backupRegion;

    public uint BaseAddress => 0x08000000;
    public uint Length => MaxSize * 3;

    internal GamePak(byte[] romData, GamePakInfo info, Action<uint> step, WaitControl waitControl, IBackupDevice? backupDevice, GPIOBus? gpio)
    {
        _addressMask = info.AddressMask;
        PakInfo = info;

        if (backupDevice != null)
        {
            _backupDevice = backupDevice;
            _isEEPROM = (backupDevice.Type & (BackupType.EEPROMDetect | BackupType.EEPROM512B | BackupType.EEPROM8K)) != 0;
            _backupRegion = new BackupRegion(this);
        }
        if (gpio != null)
        {
            _gpio = gpio;
            _isGPIO = true;
            _gpio.Reset();
        }

        ActualSize = romData.Length;
        _romMemory = new((nuint)romData.Length);
        _romMemory.WriteBytes(0, romData);

        _step = step;
        _waitControl = waitControl;

        _upperRegion = new GamePakRegion<UpperAccess>(this);
        _lowerRegion = new GamePakRegion<LowerAccess>(this);
    }

    internal IMemoryRegion GetUpperRegion()  => _upperRegion;
    internal IMemoryRegion GetLowerRegion()  => _lowerRegion;
    internal IMemoryRegion GetBackupRegion() => _backupRegion;

    internal T? GetGPIODevice<T>() where T : GPIODevice
        => _gpio.GetDevice<T>();


    public T DebugRead<T>(uint address) where T : unmanaged
    {
        const uint ROMMirrorMask = 0x0E000000;

        uint normalized = (address & ~ROMMirrorMask) | BaseAddress;
        uint offset = normalized - BaseAddress;

        if (offset + (uint)Unsafe.SizeOf<T>() <= _romMemory.Size)
            return _romMemory.Read<T>(offset);
        else
            return default!;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort ReadROM16(uint address, bool sequential)
    {
        if (!sequential)
            _romAddress = address & _addressMask;

        ushort value = _romAddress < ActualSize
            ? _romMemory.Read16(_romAddress)
            : (ushort)(_romAddress >> 1);

        _romAddress = (_romAddress + 2) & _addressMask;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint ReadROM32(uint address, bool sequential)
    {
        if (!sequential)
            _romAddress = address & _addressMask;

        uint value;
        if (_romAddress < ActualSize)
            value = _romMemory.Read32(_romAddress);
        else
        {
            ushort low = (ushort)(_romAddress >> 1);
            value = (uint)(((low + 1) << 16) | low);
        }

        _romAddress = (_romAddress + 4) & _addressMask;
        return value;
    }

    private ushort ReadData16<TAccess>(uint address, bool seq) where TAccess : struct, IAccess
    {
        address &= 0x01FFFFFE;

        if (TAccess.IsLower)
        {
            if (IsGPIOAddress(address) && _gpio.Readable)
                return _gpio.Read(address);
        }
        else if (IsEEPROMAddress(address))
        {
            return _backupDevice.Read(uint.MaxValue);
        }

        return ReadROM16(address, seq);
    }

    private uint ReadData32<TAccess>(uint address, bool seq) where TAccess : struct, IAccess
    {
        address &= 0x01FFFFFC;

        if (TAccess.IsLower)
        {
            if (IsGPIOAddress(address) && _gpio.Readable)
                return (uint)(_gpio.Read(address) | (_gpio.Read(address + 2) << 16));
        }
        else if (IsEEPROMAddress(address))
        {
            ushort lo = _backupDevice.Read(uint.MaxValue);
            ushort hi = _backupDevice.Read(uint.MaxValue);
            return (uint)(lo | (hi << 16));
        }

        return ReadROM32(address, seq);
    }


    private void WriteData16<TAccess>(uint address, bool seqAccess, ushort value) where TAccess : struct, IAccess
    {
        address &= 0x01FFFFFE; // Force align to 16-bit boundary

        if (TAccess.IsLower)
        {
            if (IsGPIOAddress(address)) _gpio.Write(address, (byte)value);

            if (!seqAccess)
                _romAddress = address & _addressMask;
        }

        else
            // EEPROM does not use the address parameter; it is a serial device. Pass in 0xFFFFFFFF to signify that it doesn't matter.
            if (IsEEPROMAddress(address)) _backupDevice.Write(0xFFFF_FFFF, (byte)value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsEEPROMAddress(uint address) => _isEEPROM && (address & _eepromMask) == _eepromMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsGPIOAddress(uint address) => _isGPIO && address <= 0xC8 && address >= 0xC4;


    public void Dispose()
    {
        _romMemory.Dispose();
        _backupDevice?.Dispose();
    }
}