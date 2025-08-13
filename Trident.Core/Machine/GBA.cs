using Trident.Core.Bus;
using Trident.Core.CPU;
using Trident.Core.Memory;
using Trident.Core.Scheduling;
using Trident.Core.Memory.GamePak;
using Trident.Core.Hardware.Controller;
using Trident.Core.Memory.GamePak.GPIO;
using Trident.Core.Hardware.Interrupts;

namespace Trident.Core.Machine
{
    public class GBA : IDisposable
    {
        internal ARM7TDMI<GBABus> CPU;
        internal InterruptController IRQController;
        private GBABusView? _busView;

        internal Scheduler Scheduler = new();

        public bool ShouldSkipBIOS = true;

        private readonly BIOS _bios;
        private readonly UnusedSection _unused = new();
        private readonly EWRAM _eWRAM = new();
        private readonly IWRAM _iWRAM = new();
        private GamePak _gamePak = null;

        public GBA()
        {
            CPU = new(Scheduler);
            IRQController = new(() => CPU.Halted = false);
            CPU.AttachIRQController(IRQController);

            CPU.AttachBus(new GBABus());
            _busView = new(ref CPU.Bus);

            MMIO mmio = new();
            mmio.AttachToBus(ref CPU.Bus);

            _bios = new(() => CPU.Registers.GetRegisterRef(15));
        }

        public T? GetGPIODevice<T>() where T : GPIODevice
            => _gamePak?.GetGPIODevice<T>();


        public void RunFor(uint cycles)
        {
            ulong runTarget = Scheduler.CurrentTimestamp + cycles;

            while (runTarget > Scheduler.CurrentTimestamp)
            {
                if (!CPU.Halted)
                    CPU.Step();
                else
                {
                    Scheduler.Step(Scheduler.RemainingCycles);

                    if (IRQController.IRQAvailable)
                    {
                        Scheduler.Step(1);
                        CPU.Halted = false;
                    }
                }
            }
        }


        public void Reset()
        {
            CPU.Reset();
            IRQController.Reset();
            // TODO: reset other components

            if (ShouldSkipBIOS)
                SkipBIOS();
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

        public void SkipBIOS()
        {
            CPU.Registers.SwitchMode(PrivilegeMode.SYS);
            CPU.Registers.SetBankForMode(PrivilegeMode.SVC, [0x03007FE0, 0]);
            CPU.Registers.SetBankForMode(PrivilegeMode.IRQ, [0x03007FA0, 0]);
            CPU.Registers.SP = 0x03007F00;
            CPU.Registers.PC = 0x08000000;
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