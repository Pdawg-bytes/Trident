using Trident.Core.Memory;
using Trident.Core.Scheduling;
using Trident.Core.Hardware.DMA;
using Trident.Core.Memory.MappedIO;
using Trident.Core.Hardware.Interrupts;

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

    private readonly LayerPixel[][] _bgLines = new LayerPixel[4][];
    private readonly LayerPixel[]   _objLine = new LayerPixel[ScreenWidth];
    private int _objDrawCycles = 0;
    private uint _pixelGeneration = 1;

    private readonly Scheduler _scheduler;

    private readonly Action<InterruptSource> _raiseIRQ;
    private readonly Action<DMATrigger, uint> _triggerDMA;

    internal PPU(Framebuffer framebuffer, PRAM pram, VRAM vram, OAM oam, Scheduler scheduler, Action<InterruptSource> raiseIRQ, Action<DMATrigger, uint> triggerDMA)
    {
        _framebuffer = framebuffer;
        _pram        = pram;
        _vram        = vram;
        _oam         = oam;
        _scheduler   = scheduler;
        _raiseIRQ    = raiseIRQ;
        _triggerDMA  = triggerDMA;

        DisplayStatus = new(() => VCount);

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

        uint scanline = VCount;

        if (scanline < 160)
        {
            byte mode = DisplayControl.BackgroundMode;
            _pixelGeneration++;

            RenderObjectLine(scanline);

            switch (mode)
            {
                case 0:
                    RenderTextBG(0, scanline);
                    RenderTextBG(1, scanline);
                    RenderTextBG(2, scanline);
                    RenderTextBG(3, scanline);
                    break;
                case 1:
                    RenderTextBG(0, scanline);
                    RenderTextBG(1, scanline);
                    RenderAffineBG<TileSampler>(2, scanline);
                    break;
                case 2:
                    RenderAffineBG<TileSampler>(2, scanline);
                    RenderAffineBG<TileSampler>(3, scanline);
                    break;

                case 3: RenderAffineBG<Bitmap3Sampler>(2, scanline); break;
                case 4: RenderAffineBG<Bitmap4Sampler>(2, scanline); break;
                case 5: RenderAffineBG<Bitmap5Sampler>(2, scanline); break;

                default: break;
            }

            CompositeScanline(scanline, mode);
        }

        _triggerDMA(DMATrigger.HBlank, VCount);

        _scheduler.Schedule(EventType.PPU_HBlankEnd, HBlankEndDelay);
        _scheduler.Schedule(EventType.PPU_SetHBlankFlag, HBlankFlagDelay);
    }

    private void OnHBlankEnd()
    {
        DisplayStatus.HBlankFlag = false;
        VCount++;

        if (VCount == 160)
            OnVBlankStart();

        if (VCount == 227)
            OnVBlankEnd();

        if (VCount == 228) 
            VCount = 0;

        if (DisplayStatus.VCountIRQ && VCount == DisplayStatus.VCountSetting)
            _raiseIRQ(InterruptSource.LCD_VCounterMatch);

        _scheduler.Schedule(EventType.PPU_HBlankStart, HBlankStartDelay);
    }


    private void OnVBlankStart()
    {
        _framebuffer.Present();

        DisplayStatus.VBlankFlag = true;

        if (DisplayStatus.VBlankIRQ)
            _raiseIRQ(InterruptSource.LCD_VBlank);

        Backgrounds[2].UpdateReferencePoints();
        Backgrounds[3].UpdateReferencePoints();

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

        for (int i = 0; i < 4; i++) 
            _bgLines[i] = new LayerPixel[ScreenWidth];

        Array.Clear(_objLine);
        _objDrawCycles = 0;

        _pixelGeneration = 1;
        ResetScanlineBuffers();

        _scheduler.Schedule(EventType.PPU_HBlankStart, 226);
    }
}