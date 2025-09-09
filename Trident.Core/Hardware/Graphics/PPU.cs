using Trident.Core.Scheduling;

namespace Trident.Core.Hardware.Graphics
{
    internal class PPU
    {
        private const int TotalScanlines = 228;
        private const int VisibleScanlines = 160;
        private const ulong HDrawCycles = 960;
        private const ulong HBlankCycles = 272;
        private const ulong ScanlineCycles = HDrawCycles + HBlankCycles;

        private int _currentScanline = 0;
        private readonly Scheduler _scheduler;

        internal PPU(Scheduler scheduler)
        {
            _scheduler = scheduler;
            // figure it out tomorrow
        }
    }
}