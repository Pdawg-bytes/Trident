using Trident.Core.Scheduling;
using Trident.Core.Hardware.DMA;
using Trident.Core.Memory.Graphics;
using Trident.Core.Memory.MappedIO;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    internal const int ScreenWidth = 240;
    internal const int ScreenHeight = 160;

    private const int HBlankStartDelay = 240 * 4; // HDraw
    private const int HBlankEndDelay = 68 * 4;    // 68px
    private const int HBlankFlagDelay = 46;       // HDraw + Delay (GBATek: "the H-Blank flag is "0" for a total of 1006 cycles.")

    private readonly Framebuffer _framebuffer;
    private readonly PRAM _pram;
    private readonly VRAM _vram;
    private readonly OAM _oam;

    private readonly Scheduler _scheduler;

    private readonly Action<InterruptSource> _raiseIRQ;
    private readonly Action<DMATrigger, uint> _triggerDMA;

    internal PPU(Framebuffer framebuffer, PRAM pram, VRAM vram, OAM oam, Scheduler scheduler, Action<InterruptSource> raiseIRQ, Action<DMATrigger, uint> triggerDMA)
    {
        _framebuffer = framebuffer;
        _pram = pram;
        _vram = vram;
        _oam = oam;
        _scheduler = scheduler;
        _raiseIRQ = raiseIRQ;
        _triggerDMA = triggerDMA;

        _scheduler.Register(EventType.PPU_HBlankStart,   OnHBlankStart);
        _scheduler.Register(EventType.PPU_HBlankEnd,     OnHBlankEnd);
        _scheduler.Register(EventType.PPU_SetHBlankFlag, () => DisplayStatus.HBlankFlag = true);
        _scheduler.Register(EventType.PPU_VBlankStart,   OnVBlankStart);
        _scheduler.Register(EventType.PPU_VBlankEnd,     OnVBlankEnd);

        Reset();
    }


    private void OnHBlankStart()
    {
        if (DisplayStatus.HBlankIRQ)
            _raiseIRQ(InterruptSource.LCD_HBlank);

        if (VCount >= 0 && VCount < 160)
        {
            // render stuff
            for (int x = 0; x < 240; x++)
            {
                ushort color = 0;
                uint bgMode = DisplayControl.BackgroundMode;

                if (bgMode == 3)
                {
                    color = _vram.Fetch<ushort>(((uint)x + VCount * 240) << 1);
                }
                else if (bgMode == 4)
                {
                    uint baseFrame = DisplayControl.FrameSelect ? 0xA000 : 0x0000u;
                    uint addr = baseFrame + (uint)(x + VCount * 240);
                    uint index = (uint)(_vram.Fetch<byte>(addr) << 1);
                    color = _pram.Fetch<ushort>(index);
                }

                _framebuffer.SetPixel(x, (int)VCount, Framebuffer.ToArgb(color));
            }
        }

        _triggerDMA(DMATrigger.HBlank, VCount);

        _scheduler.Schedule(EventType.PPU_HBlankEnd, HBlankEndDelay);
        _scheduler.Schedule(EventType.PPU_SetHBlankFlag, HBlankFlagDelay);
    }

    private void OnHBlankEnd()
    {
        DisplayStatus dispstat = DisplayStatus;

        dispstat.HBlankFlag = false;
        VCount++;

        if (VCount == 160)
            OnVBlankStart();

        if (VCount == 227)
            OnVBlankEnd();

        if (VCount == 228) 
            VCount = 0;

        if (dispstat.VCountIRQ && VCount == dispstat.VCountSetting)
            _raiseIRQ(InterruptSource.LCD_VCounterMatch);

        _scheduler.Schedule(EventType.PPU_HBlankStart, HBlankStartDelay);
    }


    private void OnVBlankStart()
    {
        DisplayStatus.VBlankFlag = true;

        if (DisplayStatus.VBlankIRQ)
            _raiseIRQ(InterruptSource.LCD_VBlank);

        _triggerDMA(DMATrigger.VBlank, 0);
    }

    private void OnVBlankEnd()
    {
        DisplayStatus.VBlankFlag = false;
    }


    internal void Reset()
    {
        DisplayControl.Write(0, WriteMask.Both);
        DisplayStatus.Write(0, WriteMask.Both);

        Greenswap = 0;
        VCount = 0;

        for (int i = 0; i < 4; i++)
            BackgroundControls[i].Write(0, WriteMask.Both);

        _scheduler.Schedule(EventType.PPU_HBlankStart, 226);
    }
}