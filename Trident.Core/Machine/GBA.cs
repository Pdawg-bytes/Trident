using Trident.Core.Bus;
using Trident.Core.CPU;
using Trident.Core.Memory;
using Trident.Core.Scheduling;
using Trident.Core.Hardware.IO;
using Trident.Core.Hardware.DMA;
using Trident.Core.CPU.Registers;
using Trident.Core.Memory.Region;
using Trident.Core.Memory.GamePak;
using Trident.Core.Memory.Graphics;
using Trident.Core.Memory.MappedIO;
using Trident.Core.Hardware.Graphics;
using System.Runtime.CompilerServices;
using Trident.Core.Memory.GamePak.GPIO;
using Trident.Core.Hardware.Controller;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Debugging.Disassembly;

namespace Trident.Core.Machine;

public sealed partial class GBA : IDisposable
{
    internal ARM7TDMI<GBABus> CPU;
    private GBABusView? _busView;

    private readonly InterruptController _irqController;
    private readonly DMAManager _dmaManager;

    public Framebuffer Framebuffer = new();
    internal PPU PPU;
    private readonly PPURegisters _ppuRegisters = new();

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

        _irqController = new(() => CPU.Halted = false, () => CPU.Halted);
        CPU.AttachIRQController(_irqController);
        CPU.AttachBreakpoints(Breakpoints);

        _dmaManager = new(_irqController.Raise, Scheduler);

        _keypad = new(_irqController.Raise);
        _postHalt = new(() => CPU.Halted = true, getPC);


        _bios  = new(getPC, Scheduler.Step);
        _eWRAM = new(Scheduler.Step);
        _iWRAM = new(Scheduler.Step);
        _pram  = new(Scheduler.Step);
        _vram  = new(Scheduler.Step, _ppuRegisters.DisplayControl.GetSpriteBoundary);
        _oam   = new(Scheduler.Step);


        BusBuilder builder = new();

        builder.Attach(MemoryRegion.BIOS,  _bios);
        builder.Attach(MemoryRegion.EWRAM, _eWRAM);
        builder.Attach(MemoryRegion.IWRAM, _iWRAM);
        builder.Attach(MemoryRegion.PRAM,  _pram);
        builder.Attach(MemoryRegion.VRAM,  _vram);
        builder.Attach(MemoryRegion.OAM,   _oam);

        _mmio = new(Scheduler.Step, _ppuRegisters, _dmaManager, _keypad, _irqController, _waitControl, _postHalt);
        builder.Attach(MemoryRegion.MMIO, _mmio);

        CPU.AttachBus(builder.Build(Scheduler.Step));
        _busView = new(ref CPU.Bus);
        _dmaManager.SetBusView(_busView);
        Disassembler = new(GetDebugRegion, getPC, GetCPUSnapshot);


        PPU = new(Framebuffer, _ppuRegisters, _pram, _vram, _oam, Scheduler, _irqController.Raise, _dmaManager.Trigger);
    }

    public T? GetGPIODevice<T>() where T : GPIODevice
        => _gamePak?.GetGPIODevice<T>();


    public void RunFor(ulong cycles)
    {
        if (IsDebuggingEnabled)
            DebugRun(cycles);
        else
            Run(cycles);
    }

    private void Run(ulong cycles)
    {
        ulong runTarget = Scheduler.CurrentTimestamp + cycles;

        while (runTarget > Scheduler.CurrentTimestamp)
        {
            if (!CPU.Halted)
                CPU.Step();
            else
                HandleHalt();
        }
    }

    private void DebugRun(ulong cycles)
    {
        ulong runTarget = Scheduler.CurrentTimestamp + cycles;

        while (runTarget > Scheduler.CurrentTimestamp)
        {
            if (!CPU.Halted)
            {
                if (!CPU.StepDebug())
                    break;
            }
            else
                HandleHalt();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleHalt()
    {
        Scheduler.SkipToNextEvent();

        if (_irqController.IRQAvailable)
        {
            Scheduler.Step(1);
            CPU.Halted = false;
        }
    }


    public void Reset()
    {
        Disassembler.Enabled = false;

        Scheduler.Reset();

        _eWRAM.Reset();
        _iWRAM.Reset();
        _pram.Reset();
        _vram.Reset();
        _oam.Reset();

        CPU.Reset();
        _irqController.Reset();
        _dmaManager.Reset();

        PPU.Reset();
        Framebuffer.Clear();

        // TODO: reset other components

        if (ShouldSkipBIOS)
            SkipBIOS();

        Disassembler.Enabled = true;
    }


    public void AttachGamePak(string filePath)
    {
        Disassembler.Enabled = false;

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

            (0x0E, backupHandler),
            (0x0F, backupHandler)
        ]);

        CPU.Bus.LoadDebugGamePak(_gamePak);

        Disassembler.Enabled = true;
    }

    public GamePakInfo GetGamePakInfo() => _gamePak.PakInfo;


    public void AttachBIOS(string path)
    {
        Disassembler.Enabled = false;

        byte[] bios = File.ReadAllBytes(path);

        if (bios.Length != BIOS.MemorySize)
            throw new Exception("BIOS image is not the correct size.");

        _bios.Clear();
        _bios.LoadBIOS(bios);

        Disassembler.Enabled = true;
    }

    public void SkipBIOS()
    {
        CPU.Registers.ClearFlag(Flags.I);
        CPU.Registers.ClearFlag(Flags.F);
        CPU.Registers.SwitchMode(ProcessorMode.SYS);
        CPU.Registers.SetBankForMode(ProcessorMode.SVC, [0x03007FE0, 0]);
        CPU.Registers.SetBankForMode(ProcessorMode.IRQ, [0x03007FA0, 0]);
        CPU.Registers.SP = 0x03007F00;
        CPU.Registers.PC = 0x08000000;
    }


    public void SetKeyState(GBAKey key, bool pressed) => _keypad.SetKeyState(key, pressed);


    public void Dispose()
    {
        Disassembler.Enabled = false;
        _busView?.Dispose();
        _busView = null;
        CPU.Bus.DisposeMemory();
    }
}