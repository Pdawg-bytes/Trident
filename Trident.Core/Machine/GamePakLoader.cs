using System.Text;
using Trident.Core.Global;
using Trident.Core.Memory.GamePak;
using System.Runtime.InteropServices;
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
            var (title, code, maker) = GetGameInfoStrings(ref header);

            BackupType backupType = GetBackupType(romData);
            IBackupDevice backupDevice = null;
            if (backupType != BackupType.None)
                backupDevice = CreateBackupDevice(saveData, backupType);
            else
                Console.WriteLine("Backup type was not able to be determined."); // TODO: replace with log

            // TODO: Load GPIO data from cartridge

            GamePakInfo info = new()
            {
                Title = title,
                Code = code,
                Maker = maker,

                Size = (uint)romData.Length,

                BackupType = backupType,
                BackupSize = backupDevice == null ? 0 : backupDevice.Size,
            };

            uint addressMask = ((uint)romData.Length).NearestPow2() - 1 /*GamePak.MaxSize - 1*/;
            // TODO: when ROM is mirrored: ((uint)romData.Length).NearestPow2() - 1

            return new GamePak(romData, addressMask, info, backupDevice, null);
        }

        private static BackupType GetBackupType(byte[] romData)
        {
            if (romData.ContainsAscii("EEPROM_V")) return BackupType.EEPROMDetect;
            if (romData.ContainsAscii("SRAM_V") || romData.ContainsAscii("SRAM_F")) return BackupType.SRAM;
            if (romData.ContainsAscii("FLASH_V") || romData.ContainsAscii("FLASH512_V")) return BackupType.Flash64K;
            if (romData.ContainsAscii("FLASH1M_V")) return BackupType.Flash128K;
            return BackupType.None;
        }

        private static IBackupDevice? CreateBackupDevice(byte[]? saveData, BackupType backupType) => backupType switch
        {
            BackupType.SRAM => new SRAM(saveData),
            _ => null,
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