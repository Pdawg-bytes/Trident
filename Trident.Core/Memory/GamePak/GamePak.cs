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


        private ushort ReadData16<TAccess>(uint address, bool seqAccess) where TAccess : struct, IAccess
        {
            address &= 0x01FF_FFFE; // Force align to 16-bit
            if (TAccess.IsLower)
            {
                // TODO: actually add reads
                if (IsGPIOAddress(address) /* && gpio is readable */) return 0x00;
            }
            else
            {
                // TOOD: actually add reads
                if (IsEEPROMAddress(address)) return 0xFF;
            }

            return _romMemory.Read16(address & _addressMask);
        }

        private uint ReadData32<TAccess>(uint address, bool seqAccess) where TAccess : struct, IAccess
        {
            address &= 0x01FF_FFFC; // Force align to 32-bit
            if (TAccess.IsLower)
            {
                // Reading 4 bytes from a GPIO address will only incorporate two GPIO registers, due to them technically being 16 bits wide;
                // however, the top 8 bits are always 0. This means that we can treat each of the two regs as ushorts and combine them.
                // TODO: attempt GPIO read
                if (IsGPIOAddress(address) /* && gpio is readable */)
                {
                    ushort low = 0xFF /*_gpio.Read(address)*/;
                    ushort top = 0x00 /*_gpio.Read(address | 2) */; // Address is aligned, therefore we can read the next register by adding 2.
                    return (uint)((top << 16) | low);
                }
            }
            else
            { 
                // TOOD: attempt EEPROM read
                if (IsEEPROMAddress(address))
                {

                }
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