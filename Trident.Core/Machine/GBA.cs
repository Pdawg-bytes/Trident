using Trident.Core.Bus;
using Trident.Core.CPU;
using Trident.Core.Memory;
using Trident.Core.Scheduling;
using Trident.Core.Hardware.IO;
using Trident.Core.Memory.Region;
using Trident.Core.Memory.GamePak;
using Trident.Core.Memory.Graphics;
using Trident.Core.Memory.MappedIO;
using Trident.Core.Hardware.Graphics;
using Trident.Core.Debugging.Snapshots;
using Trident.Core.Memory.GamePak.GPIO;
using Trident.Core.Hardware.Controller;
using Trident.Core.Hardware.Interrupts;

using Trident.Core.Debugging.Disassembly;

namespace Trident.Core.Machine
{
    public class GBA : IDisposable
    {
        internal ARM7TDMI<GBABus> CPU;
        internal InterruptController IRQController;
        private GBABusView? _busView;
        public Disassembler Disassembler;

        public Framebuffer Framebuffer = new();
        internal PPU PPU;
        private PPURegisters _ppuRegisters = new();

        internal Scheduler Scheduler = new();

        public bool ShouldSkipBIOS = true;

        private readonly BIOS _bios;

        private readonly EWRAM _eWRAM;
        private readonly IWRAM _iWRAM;

        private readonly MMIO _mmio;

        private readonly PRAM _pram;
        private readonly VRAM _vram;
        private readonly OAM _oam;

        private GamePak _gamePak = null;

        private readonly Keypad _keypad;

        private readonly WaitControl _waitControl = new();
        private readonly PostHalt _postHalt;

        public GBA()
        {
            CPU = new(Scheduler);
            Func<uint> getPC = () => CPU.Registers.GetRegisterRef(15);

            IRQController = new(() => CPU.Halted = false);
            CPU.AttachIRQController(IRQController);

            _keypad = new(IRQController.Raise);
            _postHalt = new(() => CPU.Halted = true, getPC);


            _bios = new(getPC, Scheduler.Step);
            _eWRAM = new(Scheduler.Step);
            _iWRAM = new(Scheduler.Step);
            _pram = new(Scheduler.Step);
            _vram = new(Scheduler.Step, () => _ppuRegisters.DisplayControl.BackgroundMode);
            _oam = new(Scheduler.Step);


            BusBuilder builder = new();

            builder.Attach(MemoryRegion.BIOS,  _bios);
            builder.Attach(MemoryRegion.EWRAM, _eWRAM);
            builder.Attach(MemoryRegion.IWRAM, _iWRAM);
            builder.Attach(MemoryRegion.PRAM,  _pram);
            builder.Attach(MemoryRegion.VRAM,  _vram);
            builder.Attach(MemoryRegion.OAM,   _oam);

            _mmio = new(Scheduler.Step, _ppuRegisters, _keypad, IRQController, _waitControl, _postHalt);
            builder.Attach(MemoryRegion.MMIO, _mmio);

            CPU.AttachBus(builder.Build(Scheduler.Step));
            _busView = new(ref CPU.Bus);
            Disassembler = new(GetDebugRegion, getPC, () => CPUSnapshot);


            PPU = new(Framebuffer, _ppuRegisters, _pram, _vram, _oam, Scheduler, IRQController.Raise);
        }

        public T? GetGPIODevice<T>() where T : GPIODevice
            => _gamePak?.GetGPIODevice<T>();

        public CPUSnapshot CPUSnapshot => CPU.GetSnapshot();

        private IDebugMemory? GetDebugRegion(uint region) => CPU.Bus.GetRegionAsDebug(region);


        public void RunFor(ulong cycles)
        {
            ulong runTarget = Scheduler.CurrentTimestamp + cycles;

            while (runTarget > Scheduler.CurrentTimestamp)
            {
                if (!CPU.Halted)
                    CPU.Step();
                else
                {
                    Scheduler.SkipToNextEvent();

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
            Scheduler.Reset();

            _eWRAM.Reset();
            _iWRAM.Reset();
            _pram.Reset();
            _vram.Reset();
            _oam.Reset();

            CPU.Reset();
            IRQController.Reset();

            PPU.Reset();
            Framebuffer.Clear();

            // TODO: reset other components

            if (ShouldSkipBIOS)
                SkipBIOS();
        }


        public void AttachGamePak(string filePath)
        {
            _gamePak?.Dispose();
            _gamePak = GamePakLoader.Load(File.ReadAllBytes(filePath), Scheduler.Step, _waitControl);

            IMemoryRegion gamePakUpper = _gamePak.GetUpperRegion();
            IMemoryRegion gamePakLower = _gamePak.GetLowerRegion();
            IMemoryRegion backupHandler = _gamePak.GetBackupRegion();

            CPU.Bus.RegisterHandlers
            ([
                (0x08, gamePakLower),
                (0x09, gamePakUpper),
                (0x0A, gamePakLower),
                (0x0B, gamePakUpper),
                (0x0C, gamePakLower),
                (0x0D, gamePakUpper),

                //(0x0E, backupHandler),
                //(0x0F, backupHandler)
            ]);

            CPU.Bus.LoadDebugGamePak(_gamePak);
        }

        public GamePakInfo GetGamePakInfo() => _gamePak.PakInfo;


        public void AttachBIOS(string path)
        {
            byte[] bios = File.ReadAllBytes(path);

            if (bios.Length != BIOS.MEMORY_SIZE)
                throw new Exception("BIOS image is not the correct size.");

            _bios.LoadBIOS(bios);
        }

        public void SkipBIOS()
        {
            CPU.Registers.SwitchMode(ProcessorMode.SYS);
            CPU.Registers.SetBankForMode(ProcessorMode.SVC, [0x03007FE0, 0]);
            CPU.Registers.SetBankForMode(ProcessorMode.IRQ, [0x03007FA0, 0]);
            CPU.Registers.SP = 0x03007F00;
            CPU.Registers.PC = 0x08000000;
        }


        public void SetKeyState(GBAKey key, bool pressed) => _keypad.SetKeyState(key, pressed);


        public void Dispose()
        {
            _busView?.Dispose();
            _busView = null;
            CPU.Bus.DisposeMemory();
        }
    }
}