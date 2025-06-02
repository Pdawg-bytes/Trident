using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Enums;
using Trident.Core.Memory.GamePak;
using Trident.Core.Memory.GamePak.Backup;

namespace Trident.Core.Machine
{
    internal static class GamePakLoader
    {
        internal static GamePak Load(byte[] romData, byte[]? saveData = null)
        {
            return null;
        }

        private static BackupType GetBackupType(byte[] romData) => romData switch
        {
        };

        // TODO: Move this to somewhere else
        private static bool ContainsAscii(byte[] data, string value)
        {
            if (value.Length == 0 || data.Length < value.Length)
                return false;

            byte[] needleBytes = Encoding.ASCII.GetBytes(value);
            byte firstByte = needleBytes[0];
            int maxIndex = data.Length - needleBytes.Length;

            bool CheckMatches(int offset) =>
                offset <= maxIndex && data[offset] == firstByte && needleBytes.AsSpan(1).SequenceEqual(data.AsSpan(offset + 1, needleBytes.Length - 1));

            for (int i = 0; i <= maxIndex; i++)
                if (CheckMatches(i)) return true;

            return false;
        }

        private static IBackupDevice CreateBackupDevice(byte[] saveData, BackupType backupType)
        {
            return new SRAM();
        }
    }
}