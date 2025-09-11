using Trident.Core.Bus;
using Trident.Core.Scheduling;
using Trident.Core.Memory.Graphics;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics
{
    internal class PPU
    {
        internal const int ScreenWidth = 240;
        internal const int ScreenHeight = 160;

        private const int HBlankStartDelay = 240 * 4; // HDraw
        private const int HBlankEndDelay = 68 * 4;    // 68px
        private const int HBlankFlagDelay = 46;       // HDraw + Delay (GBATek: "the H-Blank flag is "0" for a total of 1006 cycles.")

        private readonly Framebuffer _framebuffer;
        private readonly PPURegisters _registers;
        private readonly PRAM _pram;
        private readonly VRAM _vram;
        private readonly OAM _oam;

        private readonly Scheduler _scheduler;

        private readonly Action<InterruptSource> _raiseIRQ;

        internal PPU(Framebuffer framebuffer, PPURegisters registers, PRAM pram, VRAM vram, OAM oam, Scheduler scheduler, Action<InterruptSource> raiseIRQ)
        {
            _framebuffer = framebuffer;
            _registers = registers;
            _pram = pram;
            _vram = vram;
            _oam = oam;
            _scheduler = scheduler;
            _raiseIRQ = raiseIRQ;

            _scheduler.Register(EventType.PPU_HBlankStart,   OnHBlankStart);
            _scheduler.Register(EventType.PPU_HBlankEnd,     OnHBlankEnd);
            _scheduler.Register(EventType.PPU_SetHBlankFlag, () => _registers.DisplayStatus.HBlankFlag = true);
            _scheduler.Register(EventType.PPU_VBlankStart,   OnVBlankStart);
            _scheduler.Register(EventType.PPU_VBlankEnd,     OnVBlankEnd);

            Reset();
        }


        private void OnHBlankStart()
        {
            if (_registers.DisplayStatus.HBlankIrq)
                _raiseIRQ(InterruptSource.LCD_HBlank);

            if (_registers.VCount >= 0 && _registers.VCount < 160)
            {
                // render stuff
                for (int i = 0; i < 240; i++)
                {
                    ushort color = _vram.Fetch<ushort>((uint)(i + _registers.VCount * 240) * 2);
                    _framebuffer.SetPixel(i, (int)_registers.VCount, Framebuffer.ToArgb(color));
                }
            }

            _scheduler.Schedule(EventType.PPU_HBlankEnd, HBlankEndDelay);
            _scheduler.Schedule(EventType.PPU_SetHBlankFlag, HBlankFlagDelay);
        }

        private void OnHBlankEnd()
        {
            DisplayStatus dispstat = _registers.DisplayStatus;

            dispstat.HBlankFlag = false;
            _registers.VCount++;

            if (_registers.VCount == 160)
            {
                dispstat.VBlankFlag = true;
                OnVBlankStart();
            }

            if (_registers.VCount == 227)
            {
                dispstat.VBlankFlag = false;
                OnVBlankEnd();
            }

            if (_registers.VCount == 228) _registers.VCount = 0;

            if (dispstat.VCountIrq && _registers.VCount == dispstat.VCountSetting)
                _raiseIRQ(InterruptSource.LCD_VCounterMatch);

            _scheduler.Schedule(EventType.PPU_HBlankStart, HBlankStartDelay);
        }


        private void OnVBlankStart()
        {
            if (_registers.DisplayStatus.VBlankIrq)
                _raiseIRQ(InterruptSource.LCD_VBlank);
        }

        private void OnVBlankEnd()
        {
            _registers.DisplayStatus.VBlankFlag = false;
        }


        internal void Reset()
        {
            _registers.Reset();

            _scheduler.Schedule(EventType.PPU_HBlankStart, 226);
        }
    }
}