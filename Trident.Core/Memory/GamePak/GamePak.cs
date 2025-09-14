using Trident.Core.Global;
using Trident.Core.Hardware.IO;
using System.Runtime.CompilerServices;
using Trident.Core.Memory.GamePak.GPIO;
using Trident.Core.Memory.GamePak.Backup;

namespace Trident.Core.Memory.GamePak
{
    internal partial class GamePak : IDisposable
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

        private readonly MemoryAccessHandler _upperAccessHandler;
        private readonly MemoryAccessHandler _lowerAccessHandler;
        private readonly MemoryAccessHandler _backupAccessHandler = new();

        internal GamePak(byte[] romData, GamePakInfo info, Action<uint> step, WaitControl waitControl, IBackupDevice? backupDevice, GPIOBus? gpio)
        {
            _addressMask = info.AddressMask;
            PakInfo = info;

            if (backupDevice != null)
            {
                _backupDevice = backupDevice;
                _isEEPROM = (backupDevice.Type & (BackupType.EEPROMDetect | BackupType.EEPROM512B | BackupType.EEPROM8K)) != 0;
                _backupAccessHandler = InitBackupHandler();
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

            _upperAccessHandler = GetHandler<UpperAccess>();
            _lowerAccessHandler = GetHandler<LowerAccess>();
        }

        internal MemoryAccessHandler GetUpperHandler() => _upperAccessHandler;
        internal MemoryAccessHandler GetLowerHandler() => _lowerAccessHandler;
        internal MemoryAccessHandler GetBackupHandler() => _backupAccessHandler;

        internal T? GetGPIODevice<T>() where T : GPIODevice
            => _gpio.GetDevice<T>();


        internal T DebugRead<T>(uint address) where T : unmanaged
        {
            if (address + Unsafe.SizeOf<T>() < ActualSize)
                return _romMemory.Read<T>(address);
            else
                return default!;
        }

        private ushort ReadData16<TAccess>(uint address, bool seqAccess) where TAccess : struct, IAccess
        {
            address &= 0x01FFFFFE; // Force align to 16-bit boundary
            if (TAccess.IsLower)
                if (IsGPIOAddress(address) && _gpio.Readable) return _gpio.Read(address);
            else
                // EEPROM does not use the address parameter; it is a serial device. Pass in 0xFFFFFFFF to signify that it doesn't matter.
                if (IsEEPROMAddress(address)) return _backupDevice.Read(0xFFFF_FFFF);

            // Seqential accesses from one address will not overwrite the pointer! We should instead use the auto-incremented value.
            if (!seqAccess)
                _romAddress = address & _addressMask;

            ushort value;
            if (_romAddress < ActualSize)
                value = _romMemory.Read16(_romAddress);
            else
                value = (ushort)(_romAddress >> 1);

            // The GBA ROM uses an internal pointer which automatically increments.
            _romAddress = (_romAddress + sizeof(ushort)) & _addressMask;
            return value;
        }

        private uint ReadData32<TAccess>(uint address, bool seqAccess) where TAccess : struct, IAccess
        {
            address &= 0x01FFFFFC; // Force align to 32-bit boundary
            if (TAccess.IsLower)
            {
                // Reading 4 bytes from a GPIO address will only incorporate two GPIO registers, due to them technically being 16 bits wide;
                // however, the top 8 bits are always 0. This means that we can treat each of the two regs as ushorts and combine them.
                if (IsGPIOAddress(address) && _gpio.Readable)
                    // Address is aligned, therefore we can read the next register by adding 2.
                    return (uint)(((_gpio.Read(address | 2)) << 16) | _gpio.Read(address));
            }
            else
            {
                // The bus is 16-bits wide, so similarly to GPIO, we only get two values out of a 32-bit read.
                if (IsEEPROMAddress(address))
                {
                    // EEPROM does not use the address parameter; it is a serial device. Pass in uint.MaxValue to signify that it doesn't matter.
                    ushort low = _backupDevice.Read(0xFFFF_FFFF);
                    ushort top = _backupDevice.Read(0xFFFF_FFFF);
                    return (uint)((top << 16) | low);
                }
            }

            // Seqential accesses from one address will not overwrite the pointer! We should instead use the auto-incremented value.
            if (!seqAccess)
                _romAddress = address & _addressMask;

            uint value;
            if (_romAddress < ActualSize)
                value = _romMemory.Read32(_romAddress);
            else
            {
                ushort low = (ushort)(_romAddress >> 1);
                ushort top = (ushort)(low + 1);
                value = (uint)((top << 16) | low);
            }

            // The GBA ROM uses an internal pointer which automatically increments.
            _romAddress = (_romAddress + sizeof(uint)) & _addressMask;
            return value;
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
}