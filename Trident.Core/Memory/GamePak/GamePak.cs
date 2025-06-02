using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Enums;
using Trident.Core.Memory.GamePak.Backup;

namespace Trident.Core.Memory.GamePak
{
    internal class GamePak : IDisposable
    {
        internal const int MaxSize = 32 * 1024 * 1024;

        private UnsafeMemoryBlock _romMemory;
        private readonly uint _addressMask;

        private readonly IBackupDevice _backupDevice;

        internal GamePak(byte[] romData, uint addressMask, IBackupDevice? backupDevice)
        {
            _addressMask = addressMask;

            if (backupDevice != null)
                _backupDevice = backupDevice;
        }

        public void Dispose()
        {

        }
    }
}