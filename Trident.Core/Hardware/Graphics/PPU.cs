using Trident.Core.Scheduling;
using Trident.Core.Hardware.DMA;
using Trident.Core.Memory.Graphics;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal class PPU
{
    internal const int ScreenWidth = 240;
    internal const int ScreenHeight = 160;

    private const int HBlankStartDelay = 240 * 4; // HDraw
    private const int HBlankEndDelay = 68 * 4;    // 68px
    private const int HBlankFlagDelay = 46;       // HDraw + Delay (GBATek: "the H-Blank flag is "0" for a total of 1006 cycles.")

    private readonly Framebuffer _framebuffer;
    private readonly PPURegisters _state;
    private readonly PRAM _pram;
    private readonly VRAM _vram;
    private readonly OAM _oam;

    private readonly Scheduler _scheduler;

    private readonly Action<InterruptSource> _raiseIRQ;
    private readonly Action<DMATrigger, uint> _triggerDMA;

    internal PPU(Framebuffer framebuffer, PPURegisters registers, PRAM pram, VRAM vram, OAM oam, Scheduler scheduler, Action<InterruptSource> raiseIRQ, Action<DMATrigger, uint> triggerDMA)
    {
        _framebuffer = framebuffer;
        _state = registers;
        _pram = pram;
        _vram = vram;
        _oam = oam;
        _scheduler = scheduler;
        _raiseIRQ = raiseIRQ;
        _triggerDMA = triggerDMA;

        _scheduler.Register(EventType.PPU_HBlankStart,   OnHBlankStart);
        _scheduler.Register(EventType.PPU_HBlankEnd,     OnHBlankEnd);
        _scheduler.Register(EventType.PPU_SetHBlankFlag, () => _state.DisplayStatus.HBlankFlag = true);
        _scheduler.Register(EventType.PPU_VBlankStart,   OnVBlankStart);
        _scheduler.Register(EventType.PPU_VBlankEnd,     OnVBlankEnd);

        Reset();
    }


    private void OnHBlankStart()
    {
        if (_state.DisplayStatus.HBlankIRQ)
            _raiseIRQ(InterruptSource.LCD_HBlank);

        if (_state.VCount >= 0 && _state.VCount < 160)
        {
            // render stuff
            for (int x = 0; x < 240; x++)
            {
                ushort color = 0;
                uint bgMode = _state.DisplayControl.BackgroundMode;

                if (bgMode == 3)
                {
                    color = _vram.Fetch<ushort>(((uint)x + _state.VCount * 240) << 1);
                }
                else if (bgMode == 4)
                {
                    uint baseFrame = _state.DisplayControl.FrameSelect ? 0xA000 : 0x0000u;
                    uint addr = baseFrame + (uint)(x + _state.VCount * 240);
                    uint index = (uint)(_vram.Fetch<byte>(addr) << 1);
                    color = _pram.Fetch<ushort>(index);
                }

                _framebuffer.SetPixel(x, (int)_state.VCount, Framebuffer.ToArgb(color));
            }
        }

        _triggerDMA(DMATrigger.HBlank, _state.VCount);

        _scheduler.Schedule(EventType.PPU_HBlankEnd, HBlankEndDelay);
        _scheduler.Schedule(EventType.PPU_SetHBlankFlag, HBlankFlagDelay);
    }

    private void OnHBlankEnd()
    {
        DisplayStatus dispstat = _state.DisplayStatus;

        dispstat.HBlankFlag = false;
        _state.VCount++;

        if (_state.VCount == 160)
            OnVBlankStart();

        if (_state.VCount == 227)
            OnVBlankEnd();

        if (_state.VCount == 228) 
            _state.VCount = 0;

        if (dispstat.VCountIRQ && _state.VCount == dispstat.VCountSetting)
            _raiseIRQ(InterruptSource.LCD_VCounterMatch);

        _scheduler.Schedule(EventType.PPU_HBlankStart, HBlankStartDelay);
    }


    private void OnVBlankStart()
    {
        _state.DisplayStatus.VBlankFlag = true;

        if (_state.DisplayStatus.VBlankIRQ)
            _raiseIRQ(InterruptSource.LCD_VBlank);

        _triggerDMA(DMATrigger.VBlank, 0);
    }

    private void OnVBlankEnd()
    {
        _state.DisplayStatus.VBlankFlag = false;
    }


    internal void Reset()
    {
        _state.Reset();

        _scheduler.Schedule(EventType.PPU_HBlankStart, 226);
    }
}