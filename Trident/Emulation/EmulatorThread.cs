using Trident.Commands;
using System.Diagnostics;
using Trident.Core.Machine;
using System.Collections.Concurrent;
using Trident.Core.Hardware.Controller;

namespace Trident.Emulation;

public class EmulatorThread
{
    private GBA _gba;
    private Thread _thread;

    private volatile bool _initialized;
    private volatile bool _paused = true;
    private volatile bool _speedCapped = true;

    internal const uint CyclesPerFrame = 280_896;
    internal const uint CyclesPerScanline = 1232;
    internal const double Framerate = 59.7374117;
    internal const double TargetFrametime = 1.0 / Framerate;

    private readonly Stopwatch _frameTimer = Stopwatch.StartNew();
    private readonly FrameCounter _frameCounter = new(50);
    private double _nextFrameTime = 0;
    private int _lastPresentedFrameId;

    internal double CurrentSpeed => _frameCounter.GetFPS() / Framerate * 100.0;

    // Handle keypresses in a standalone queue to decrease latency and avoid boxing command objects.
    private readonly ConcurrentQueue<KeyPressedCommand> _keyQueue = new();
    private readonly ConcurrentQueue<IEmulatorCommand> _generalQueue = new();

    internal EmulatorThread(GBA gba)
    {
        _gba = gba;
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
    }


    // Can't use a property since the backing field is volatile.
    internal bool IsPaused() => _paused;
    internal void SetPause(bool shouldPause)
    {
        _paused = shouldPause;

        if (shouldPause)
            _frameCounter.Reset();
    }

    // Can't use a property since the backing field is volatile.
    internal bool IsSpeedCapped() => _speedCapped;
    internal void SetSpeedCap(bool shouldCapSpeed)
    {
        _speedCapped = shouldCapSpeed;

        if (shouldCapSpeed)
            _nextFrameTime = _frameTimer.Elapsed.TotalSeconds;
    }

    internal bool ShouldSkipBIOS
    {
        get => _gba.ShouldSkipBIOS;
        set => _gba.ShouldSkipBIOS = value;
    }


    internal void Start()
    {
        if (_initialized)
            throw new InvalidOperationException("Attempted to re-intialize EmulatorThread.");

        _initialized = true;
        _thread = new Thread(RunLoop);
        _thread.Start();
        _initialized = true;
    }

    internal void Stop()
    {
        if (_initialized)
        {
            _paused = _speedCapped = true;
            _initialized = false;
            _frameCounter.Reset();
            _thread.Join();
        }
    }

    private void RunLoop()
    {
        _gba.Reset();
        _lastPresentedFrameId = _gba.Framebuffer.PresentedFrameId;

        while (_initialized)
        {
            ProcessCommands();

            if (!_paused)
            {
                _gba.RunFor(CyclesPerFrame);

                int presentedFrameId = _gba.Framebuffer.PresentedFrameId;
                while (_lastPresentedFrameId < presentedFrameId)
                {
                    _frameCounter.FrameRendered();
                    _lastPresentedFrameId++;
                }
            }

            if (_speedCapped)
            {
                double timeLeft = _nextFrameTime - _frameTimer.Elapsed.TotalSeconds;

                if (timeLeft > 0)
                    OpenTK.Core.Utils.AccurateSleep(timeLeft, _schedulerPeriod);

                _nextFrameTime += TargetFrametime;
            }
        }
    }

    private int _schedulerPeriod = 8;
    private bool _accurateSleep;
    internal bool AccurateSleep
    {
        get => _accurateSleep;
        set
        {
            _accurateSleep = value;
            _schedulerPeriod = value ? 0 : 8;
        }
    }


    internal void Reset() => EnqueueCommand(new ResetCommand());
    internal void KeyEvent(GBAKey key, bool pressed) => EnqueueCommand(new KeyPressedCommand(key, pressed));


    internal void EnqueueCommand(IEmulatorCommand command) => _generalQueue.Enqueue(command);
    internal void EnqueueCommand(KeyPressedCommand command)
    {
        if (_paused) return;
        _keyQueue.Enqueue(command);
    }

    private void ProcessCommands()
    {
        while (_keyQueue.TryDequeue(out var keyCmd))
            keyCmd.Execute(_gba, this);

        while (_generalQueue.TryDequeue(out var command))
            command.Execute(_gba, this);
    }
}