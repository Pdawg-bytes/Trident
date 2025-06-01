using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Enums;
using Trident.Core.Memory.GamePak.Backup;

namespace Trident.Core.Memory.GamePak
{
    public class GamePak : IDisposable
    {
        private UnsafeMemoryBlock _romMemory;
        private readonly IBackupDevice _backupDevice;

        public GamePak(byte[] rom, BackupType backupType, byte[] saveData = null)
        {

        }

        public void Dispose()
        {

        }
    }
}