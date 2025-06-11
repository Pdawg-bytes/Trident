using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Enums;
using Trident.Core.Global;
using Trident.Core.Memory.GamePak;
using Trident.Core.Memory.GamePak.Backup;

namespace Trident.Core.Machine
{
    internal static class GamePakLoader
    {
        internal unsafe static GamePak Load(byte[] romData, byte[]? saveData = null)
        {
            if (romData.Length < ROMHeader.Size || romData.Length > GamePak.MaxSize)
                throw new ArgumentException("ROM file is either too small or too large.");

            ROMHeader header = GetHeader(romData);
            var gameIDs = GetGameInfoStrings(ref header);

            BackupType backupType = GetBackupType(romData);
            IBackupDevice backupDevice = null;
            if (backupType != BackupType.None && saveData != null)
                backupDevice = CreateBackupDevice(saveData, backupType);
            else if (backupType == BackupType.None)
                Console.WriteLine("Backup type was not able to be determined."); // TODO: replace with log

            // TODO: GPIO

            uint addressMask = GamePak.MaxSize - 1;
            // TODO: when ROM is mirrored: ((uint)romData.Length).NearestPow2() - 1

            return new GamePak(romData, addressMask, backupDevice);
        }

        private static BackupType GetBackupType(byte[] romData)
        {
            if (romData.ContainsAscii("EEPROM_V")) return BackupType.EEPROMDetect;
            if (romData.ContainsAscii("SRAM_V") || romData.ContainsAscii("SRAM_F")) return BackupType.SRAM;
            if (romData.ContainsAscii("FLASH_V") || romData.ContainsAscii("FLASH512_V")) return BackupType.Flash64K;
            if (romData.ContainsAscii("FLASH1M_V")) return BackupType.Flash128K;
            return BackupType.None;
        }

        private static IBackupDevice CreateBackupDevice(byte[] saveData, BackupType backupType) => backupType switch
        {
            BackupType.SRAM => new SRAM(saveData),
            _ => throw new NotImplementedException($"The backup device {backupType} is not currently implemented.")
        };

        private static unsafe ROMHeader GetHeader(byte[] romData)
        {
            if (romData.Length < sizeof(ROMHeader))
                throw new ArgumentException("ROM data too small.");

            fixed (byte* ptr = &romData[0])
                return *(ROMHeader*)ptr;
        }

        static unsafe (string title, string code, string maker) GetGameInfoStrings(ref ROMHeader header)
        {
            ReadOnlySpan<byte> title = MemoryMarshal.CreateReadOnlySpan(ref header.GameInfo.Title[0], 12);
            ReadOnlySpan<byte> code = MemoryMarshal.CreateReadOnlySpan(ref header.GameInfo.Code[0], 4);
            ReadOnlySpan<byte> maker = MemoryMarshal.CreateReadOnlySpan(ref header.GameInfo.Maker[0], 2);

            return (
                Encoding.ASCII.GetString(title).TrimEnd('\0'),
                Encoding.ASCII.GetString(code).TrimEnd('\0'),
                Encoding.ASCII.GetString(maker).TrimEnd('\0')
            );
        }
    }
}