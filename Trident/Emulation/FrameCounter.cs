using System.Diagnostics;

namespace Trident.Emulation
{
    internal class FrameCounter
    {
        private int _frameCount = 0;
        private double _lastFps = 0;
        private double _lastUpdateTime = 0;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly double _updateIntervalSeconds;

        internal FrameCounter(int updateIntervalMs) => _updateIntervalSeconds = updateIntervalMs / 1000.0;

        public void FrameRendered()
        {
            _frameCount++;

            double currentTime = _stopwatch.Elapsed.TotalSeconds;
            if (currentTime - _lastUpdateTime >= _updateIntervalSeconds)
            {
                double fps = _frameCount / (currentTime - _lastUpdateTime);
                Interlocked.Exchange(ref _frameCount, 0);
                _lastUpdateTime = currentTime;

                Volatile.Write(ref _lastFps, fps);
            }
        }

        public void Reset() => _lastFps = 0;

        public double GetFPS() => Volatile.Read(ref _lastFps);
    }
}