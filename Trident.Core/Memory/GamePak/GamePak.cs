using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Enums;
using Trident.Core.Memory.GamePak.Backup;

namespace Trident.Core.Memory.GamePak
{
    internal partial class GamePak : IDisposable
    {
        internal const int MaxSize = 32 * 1024 * 1024;

        private UnsafeMemoryBlock _romMemory;
        private readonly uint _addressMask;

        private readonly IBackupDevice _backupDevice;

        private readonly MemoryAccessHandler _upperAccessHandler;
        private readonly MemoryAccessHandler _lowerAccessHandler;
        private readonly MemoryAccessHandler _backupAccessHandler;

        internal GamePak(byte[] romData, uint addressMask, IBackupDevice? backupDevice)
        {
            _addressMask = addressMask;

            if (backupDevice != null)
            {
                _backupDevice = backupDevice;
                _backupAccessHandler = _backupDevice.GetAccessHandler();
            }

            _romMemory = new((nuint)romData.Length);
            _romMemory.WriteBytes(0, romData);

            _upperAccessHandler = GetHandler<UpperAccess>();
            _lowerAccessHandler = GetHandler<LowerAccess>();
        }

        internal MemoryAccessHandler GetUpperHandler() => _upperAccessHandler;
        internal MemoryAccessHandler GetLowerHandler() => _lowerAccessHandler;
        internal MemoryAccessHandler GetBackupHandler() => _backupAccessHandler;


        private ushort ReadData16<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
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

        private uint ReadData32<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
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

        public void Dispose()
        {
            _romMemory.Dispose();
        }
    }
}