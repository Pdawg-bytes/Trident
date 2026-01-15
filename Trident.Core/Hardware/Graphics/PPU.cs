using Trident.Core.Scheduling;
using Trident.Core.Hardware.DMA;
using Trident.Core.Memory.Graphics;
using Trident.Core.Memory.MappedIO;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Hardware.Graphics.Registers;

namespace Trident.Core.Hardware.Graphics;

internal partial class PPU
{
    internal const int ScreenWidth  = 240;
    internal const int ScreenHeight = 160;

    private const int HBlankStartDelay = 240 * 4; // HDraw
    private const int HBlankEndDelay   = 68 * 4;  // 68px
    private const int HBlankFlagDelay  = 46;      // HDraw + Delay (GBATek: "the H-Blank flag is "0" for a total of 1006 cycles.")

    private readonly Framebuffer _framebuffer;
    private readonly PRAM _pram;
    private readonly VRAM _vram;
    private readonly OAM _oam;

    internal Background[] Backgrounds =
    [
        new(0),
        new(1),
        new(2),
        new(3)
    ];

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

        if (VCount < 160)
        {
            // TODO: Clear lines if needed

            //RenderObjectLine();

            switch (DisplayControl.BackgroundMode)
            {
                case 0 or 1 or 2:
                    break;

                case 3: RenderMode3BG(); break;
                case 4: RenderMode4BG(); break;
                case 5: RenderMode5BG(); break;
            }

            //Composite();
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
            Backgrounds[i].Reset();

        _scheduler.Schedule(EventType.PPU_HBlankStart, 226);
    }
}