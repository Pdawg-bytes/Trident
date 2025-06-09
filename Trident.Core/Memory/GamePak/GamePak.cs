using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Enums;
using Trident.Core.Memory.GamePak.Backup;

namespace Trident.Core.Memory.GamePak
{
    internal partial class GamePak : IDisposable
    {
        internal const int MaxSize = 32 * 1024 * 1024;
        internal readonly int ActualSize;

        private UnsafeMemoryBlock _romMemory;
        private readonly uint _addressMask;
        private uint _romAddress;

        private readonly IBackupDevice _backupDevice;
        private readonly bool _isEEPROM;
        private readonly uint _eepromMask;

        private readonly bool _isGPIO;

        private readonly MemoryAccessHandler _upperAccessHandler;
        private readonly MemoryAccessHandler _lowerAccessHandler;
        private readonly MemoryAccessHandler _backupAccessHandler;

        internal GamePak(byte[] romData, uint addressMask, IBackupDevice? backupDevice)
        {
            _addressMask = addressMask;

            if (backupDevice != null)
            {
                _backupDevice = backupDevice;
                _isEEPROM = backupDevice.Type == BackupType.EEPROMDetect;
                _backupAccessHandler = _backupDevice.GetAccessHandler();
            }

            _isGPIO = false;

            ActualSize = romData.Length;
            _romMemory = new((nuint)romData.Length);
            _romMemory.WriteBytes(0, romData);

            _upperAccessHandler = GetHandler<UpperAccess>();
            _lowerAccessHandler = GetHandler<LowerAccess>();
        }

        internal MemoryAccessHandler GetUpperHandler() => _upperAccessHandler;
        internal MemoryAccessHandler GetLowerHandler() => _lowerAccessHandler;
        internal MemoryAccessHandler GetBackupHandler() => _backupAccessHandler;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte? AttemptSpecialRead<TAccess>(uint address) where TAccess : struct, IAccess
        {
            if (TAccess.IsLower)
            {
                // TODO: GPIO read
                return 0xFF;
            }
            else
            {
                // TODO: attempt EEPROM read
                return 0x00;
            }
        }

        private byte ReadData8<TAccess>(uint address, bool seqAccess) where TAccess : struct, IAccess
        {
            byte? specialRead = AttemptSpecialRead<TAccess>(address);
            if (specialRead != null)
                return (byte)specialRead;

            return _romMemory.Read8(address & _addressMask);
        }

        private ushort ReadData16<TAccess>(uint address, bool seqAccess) where TAccess : struct, IAccess
        {
            if (TAccess.IsLower)
            {
                // TODO: attempt GPIO read
            }
            else
            {
                // TOOD: attempt EEPROM read
            }

            return _romMemory.Read16(address & _addressMask);
        }

        private uint ReadData32<TAccess>(uint address, bool seqAccess) where TAccess : struct, IAccess
        {
            if (TAccess.IsLower)
            {
                // TODO: attempt GPIO read
            }
            else
            {
                // TOOD: attempt EEPROM read
            }

            return _romMemory.Read32(address & _addressMask);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsEEPROMAddress(uint address) => _isEEPROM && (address & _eepromMask) == _eepromMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsGPIOAddress(uint address) => _isGPIO && address <= 0xC8 && address >= 0xC4;

        public void Dispose()
        {
            _romMemory.Dispose();
        }
    }
}