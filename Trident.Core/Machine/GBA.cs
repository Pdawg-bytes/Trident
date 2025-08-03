using Trident.Core.Bus;
using Trident.Core.CPU;
using Trident.Core.Memory;
using Trident.Core.Memory.GamePak;
using Trident.Core.Devices.Controller;
using Trident.Core.Memory.GamePak.GPIO;

namespace Trident.Core.Machine
{
    public class GBA : IDisposable
    {
        internal ARM7TDMI<GBABus> CPU;
        private GBABusView? _busView;

        private readonly BIOS _bios;
        private readonly UnusedSection _unused = new();
        private readonly EWRAM _eWRAM = new();
        private readonly IWRAM _iWRAM = new();
        private GamePak _gamePak = null;

        public GBA()
        {
            CPU = new();

            _bios = new(() => CPU.Registers.GetRegisterRef(15));

            CPU.AttachBus(new GBABus());
            _busView = new(ref CPU.Bus);
        }

        public T? GetGPIODevice<T>() where T : GPIODevice
            => _gamePak?.GetGPIODevice<T>();

        public void Reset()
        {

        }


        public void AttachGamePak(string filePath)
        {
            _gamePak?.Dispose();
            _gamePak = GamePakLoader.Load(File.ReadAllBytes(filePath));

            MemoryAccessHandler gamePakUpper = _gamePak.GetUpperHandler();
            MemoryAccessHandler gamePakLower = _gamePak.GetLowerHandler();
            MemoryAccessHandler backupHandler = _gamePak.GetBackupHandler();

            CPU.Bus.RegisterHandlers
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
            CPU.Bus.RegisterHandler(0x00, _bios.GetAccessHandler());
        }


        public void SetKeyState(GBAKey key, bool pressed) { }


        public void Dispose()
        {
            _busView?.Dispose();
            _busView = null;
            CPU.Bus.DisposeMemory();
        }
    }
}