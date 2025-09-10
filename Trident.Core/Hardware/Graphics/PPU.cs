using Trident.Core.Scheduling;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Memory.MappedIO;

namespace Trident.Core.Hardware.Graphics
{
    internal class PPU
    {
        private const int HBlankStartDelay = 240 * 4; // HDraw
        private const int HBlankEndDelay = 68 * 4;    // 68px
        private const int HBlankFlagDelay = 46;       // HDraw + Delay (GBATek: "the H-Blank flag is "0" for a total of 1006 cycles.")

        private int scanline = 0;

        private readonly Scheduler _scheduler;

        private readonly Action<InterruptSource> _raiseIRQ;

        internal PPU(Scheduler scheduler, Action<InterruptSource> raiseIRQ)
        {
            _scheduler = scheduler;
            _raiseIRQ = raiseIRQ;

            _scheduler.Register(EventType.PPU_HBlankStart,   OnHBlankStart);
            _scheduler.Register(EventType.PPU_HBlankEnd,     OnHBlankEnd);
            _scheduler.Register(EventType.PPU_SetHBlankFlag, SetHBlankFlag);
            _scheduler.Register(EventType.PPU_VBlankStart,   OnVBlankStart);
            _scheduler.Register(EventType.PPU_VBlankEnd,     OnVBlankEnd);
        }


        private void OnHBlankStart()
        {
            // TODO
            //if (DISPSTAT hbl irq)
            //    _raiseIRQ(InterruptSource.LCD_HBlank);

            if (scanline >= 0 && scanline < 160)
            {
                // render stuff
            }

            _scheduler.Schedule(EventType.PPU_HBlankEnd, HBlankEndDelay);
            _scheduler.Schedule(EventType.PPU_SetHBlankFlag, HBlankFlagDelay);
        }

        private void OnHBlankEnd()
        {
            // TODO: Clear DISPSTAT hbl
            scanline++;

            if (scanline == 160)
            {
                // TODO: Set DISPSTAT vbl
                OnVBlankStart();
            }

            if (scanline == 227)
            {
                // TODO: Clear DISPSTAT vbl
                OnVBlankEnd();
            }

            if (scanline == 228) scanline = 0;

            // TODO
            //if (DISPSTAT vcnt irq && scanline == MMIO vcount)
            //    _raiseIRQ(InterruptSource.LCD_VCounterMatch);

            _scheduler.Schedule(EventType.PPU_HBlankStart, HBlankStartDelay);
        }

        private void SetHBlankFlag()
        {
            // TOD: Set DISPSTAT hbl
        }

        private void OnVBlankStart()
        {
            // TODO
            //if (DISPSTAT vbl irq)
            //    _raiseIRQ(InterruptSource.LCD_VBlank);
        }

        private void OnVBlankEnd()
        {
            // TODO: Clear DISPSTAT vbl
        }
    }
}