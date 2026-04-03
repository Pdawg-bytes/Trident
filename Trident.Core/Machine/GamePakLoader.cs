using System.Text;
using Trident.Core.Global;
using Trident.Core.Hardware.IO;
using Trident.Core.Memory.GamePak;
using Trident.Core.Memory.GamePak.Backup;

namespace Trident.Core.Machine;

internal static class GamePakLoader
{
    internal static unsafe GamePak Load(byte[] romData, Action<uint> step, WaitControl waitControl, byte[]? saveData = null)
    {
        if (romData.Length < ROMHeader.Size || romData.Length > GamePak.MaxSize)
            throw new ArgumentException("ROM file is either too small or too large.");

        fixed (byte* ptr = romData)
        {
            ref ROMHeader header = ref *(ROMHeader*)ptr;
            
            if (!header.IsValid())
                Console.WriteLine("Warning: ROM header checksum is invalid!");

            var (title, code, maker) = header.GetGameInfoStrings();

            BackupType backupType       = DetectBackupType(romData);
            IBackupDevice? backupDevice = CreateBackupDevice(backupType, saveData);
            // TODO: when ROM is mirrored: ((uint)romData.Length).NearestPow2() - 1
            uint romMask                = GamePak.MaxSize - 1;

            GamePakInfo info = new()
            {
                Title       = title,
                Code        = code,
                Maker       = maker,
                Size        = (uint)romData.Length,
                BackupType  = backupType,
                BackupSize  = backupDevice?.Size ?? 0,
                AddressMask = romMask
            };

            return new GamePak(romData, info, step, waitControl, backupDevice, null);
        }
    }

    private static BackupType DetectBackupType(byte[] romData)
    {
        ReadOnlySpan<(string signature, BackupType type)> signatures =
        [
            ("EEPROM_V",   BackupType.EEPROMDetect),
            ("SRAM_V",     BackupType.SRAM),
            ("SRAM_F_V",   BackupType.SRAM),
            ("FLASH_V",    BackupType.Flash64K),
            ("FLASH512_V", BackupType.Flash64K),
            ("FLASH1M_V",  BackupType.Flash128K)
        ];

        ReadOnlySpan<byte> data = romData.AsSpan();
        foreach (var (signature, type) in signatures)
        {
            if (data.ContainsAscii(signature, step: 4))
                return type;
        }

        return BackupType.None;
    }

    private static IBackupDevice? CreateBackupDevice(BackupType backupType, byte[]? saveData)
    {
        return backupType switch
        {
            BackupType.SRAM         => new SRAM(saveData),
            BackupType.EEPROMDetect => new EEPROM(BackupType.EEPROM8K, saveData),   // TODO: use heuristic to determine between 512B and 8K
            BackupType.EEPROM512B   => new EEPROM(BackupType.EEPROM512B, saveData),
            BackupType.EEPROM8K     => new EEPROM(BackupType.EEPROM8K, saveData),
            BackupType.Flash64K     => new Flash(BackupType.Flash64K, saveData),
            BackupType.Flash128K    => new Flash(BackupType.Flash128K, saveData),
            _ => null
        };
    }
}