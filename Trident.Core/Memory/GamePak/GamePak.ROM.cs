using Trident.Core.CPU;
using Trident.Core.Global;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.GamePak;

internal sealed partial class GamePak
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WaitAccess16(uint address, bool seqAccess)
    {
        uint region = (address >> 25) & 0b11;
        _step(_waitControl.AccessTimings16[seqAccess ? 1 : 0][region]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WaitAccess32(uint address, bool seqAccess)
    {
        uint region = (address >> 25) & 0b11;
        _step(_waitControl.AccessTimings32[seqAccess ? 1 : 0][region]);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort ReadROM16(uint address, bool sequential)
    {
        if (!sequential)
            _romAddress = address & _romAddressMask;

        ushort value = _romAddress < ActualSize
            ? _romMemory.Read16(_romAddress)
            : (ushort)(_romAddress >> 1);

        _romAddress = (_romAddress + 2) & _romAddressMask;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint ReadROM32(uint address, bool sequential)
    {
        if (!sequential)
            _romAddress = address & _romAddressMask;

        uint value;
        if (_romAddress < ActualSize)
        {
            value = _romMemory.Read32(_romAddress);
        }
        else
        {
            ushort low = (ushort)(_romAddress >> 1);
            value      = (uint)(((ushort)(low + 1) << 16) | low);
        }

        _romAddress = (_romAddress + 4) & _romAddressMask;
        return value;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort ReadData16(uint address, bool seq, bool isLower)
    {
        address &= 0x01FFFFFE;

        if (isLower && IsGPIOAddress(address) && _gpio!.Readable)
            return _gpio.Read(address);

        if (!isLower && IsEEPROMAddress(address))
            return _backupDevice!.Read(uint.MaxValue);

        return ReadROM16(address, seq);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteData16(uint address, bool seqAccess, ushort value, bool isLower)
    {
        address &= 0x01FFFFFE;

        if (isLower)
        {
            if (IsGPIOAddress(address))
                _gpio!.Write(address, (byte)value);

            if (!seqAccess)
                _romAddress = address & _romAddressMask;
        }
        else
        {
            if (IsEEPROMAddress(address))
                _backupDevice!.Write(uint.MaxValue, (byte)(value & 1));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint ReadData32(uint address, bool seq, bool isLower)
    {
        address &= 0x01FFFFFC;

        if (isLower && IsGPIOAddress(address) && _gpio!.Readable)
            return (uint)(_gpio.Read(address) | (_gpio.Read(address + 2) << 16));

        if (!isLower && IsEEPROMAddress(address))
        {
            ushort lo = _backupDevice!.Read(uint.MaxValue);
            ushort hi = _backupDevice!.Read(uint.MaxValue);
            return (uint)(lo | (hi << 16));
        }

        return ReadROM32(address, seq);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte ReadBackup(uint address)
    {
        _step(_waitControl.AccessTimings16[0][3]);
        return _backupDevice!.Read(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteBackup(uint address, byte value)
    {
        _step(_waitControl.AccessTimings16[0][3]);
        _backupDevice!.Write(address & 0x0EFFFFFF, value);
    }
}