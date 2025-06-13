using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Core.Bus;
using Trident.Core.CPU;
using Trident.Core.Memory;
using Trident.Core.Memory.GamePak;
using Trident.Core.Memory.GamePak.GPIO;

namespace Trident.Core.Machine
{
    public class GBA
    {
        internal ARM7TDMI CPU;
        internal DataBus Bus;

        private readonly BIOS _bios;
        private readonly UnusedSection _unused = new();
        private readonly EWRAM _eWRAM = new();
        private readonly IWRAM _iWRAM = new();
        private GamePak _gamePak = null;

        public GBA()
        {
            CPU = new();
            Bus = new DataBus();

            _bios = new(() => CPU.Registers.GetRegisterRef(15));

            CPU.AttachBus(Bus);
        }


        public T? GetGPIODevice<T>() where T : GPIODevice
            => _gamePak?.GetGPIODevice<T>();

        public void AttachGamePak(string filePath)
        {
            _gamePak?.Dispose();
            _gamePak = GamePakLoader.Load(File.ReadAllBytes(filePath));

            MemoryAccessHandler gamePakUpper = _gamePak.GetUpperHandler();
            MemoryAccessHandler gamePakLower = _gamePak.GetLowerHandler();
            MemoryAccessHandler backupHandler = _gamePak.GetBackupHandler();

            Bus.RegisterHandlers
            ([
                (0x08, gamePakLower),
                (0x09, gamePakUpper),
                (0x0A, gamePakLower),
                (0x0B, gamePakUpper),
                (0x0C, gamePakLower),
                (0x0D, gamePakUpper),

                (0x0E, backupHandler),
                (0x0F, backupHandler)
            ]);
        }

        public GamePakInfo GetGamePakInfo() => _gamePak.PakInfo;


        public void AttachBIOS(byte[] bios)
        {
            if (bios.Length != BIOS.MEMORY_SIZE)
                throw new Exception("BIOS image is not the correct size.");

            _bios.LoadBIOS(bios);
        }
    }
}