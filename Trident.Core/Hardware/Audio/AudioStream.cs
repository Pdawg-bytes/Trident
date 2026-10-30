using System.Diagnostics;

namespace Trident.Core.Hardware.Audio;

public sealed class AudioStream
{
    public const int Channels = 2;

    private const int BaseRate       = 32768;
    private const int MaxQueued      = 1 << 15;
    private const int SignalInterval = 128;

    private const double TargetLatency = 0.05;
    private const double MaxLatency    = 0.12;
    private const double MaxRateAdjust = 0.01;
    private const double SlowSpeed     = 0.9;
    private const double SpeedWindow   = 0.25;
    private const float GainStep       = 1f / 128;

    private readonly uint[] _ring = new uint[MaxQueued];
    private int _head;
    private int _tail;

    private readonly AutoResetEvent _dataAvailable = new(false);

    private long _written;
    private long _speedWritten;
    private long _speedStamp = Stopwatch.GetTimestamp();
    private double _speed    = 1.0;

    private int _sinceSignal;
    private volatile int _rateShift;
    private volatile bool _clearRequested;

    private int _underruns;
    private int _drains;

    private readonly float[] _left  = new float[4];
    private readonly float[] _right = new float[4];
    private double _fraction        = 1.0;

    private bool _ready;
    private short _lastLeft;
    private short _lastRight;
    private float _gain;

    public int SampleRate { get; }

    public int Underruns => Volatile.Read(ref _underruns);
    public int Drains    => Volatile.Read(ref _drains);
    private int Count    => Volatile.Read(ref _tail) - Volatile.Read(ref _head);

    public AudioStream(int sampleRate = 48000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        SampleRate = sampleRate;
    }


    internal void Write(short left, short right, int rateShift)
    {
        int tail = _tail;
        if (tail - Volatile.Read(ref _head) >= MaxQueued)
            return;

        _rateShift = rateShift;
        _ring[tail & (MaxQueued - 1)] = (ushort)left | ((uint)(ushort)right << 16);
        Volatile.Write(ref _tail, tail + 1);
        Volatile.Write(ref _written, _written + 1);

        if (++_sinceSignal >= SignalInterval)
        {
            _sinceSignal = 0;
            _dataAvailable.Set();
        }
    }

    internal void RequestClear()
    {
        _clearRequested = true;
        _dataAvailable.Set();
    }


    public bool Wait(int timeoutMilliseconds) => _dataAvailable.WaitOne(timeoutMilliseconds);

    public void Read(Span<short> destination)
    {
        if (_clearRequested)
        {
            _clearRequested = false;
            Drain(0);

            Array.Clear(_left);
            Array.Clear(_right);

            _fraction     = 1.0;
            _ready        = false;
            _speed        = 1.0;
            _speedStamp   = Stopwatch.GetTimestamp();
            _speedWritten = Volatile.Read(ref _written);
        }

        double inputRate = BaseRate << _rateShift;
        double target    = TargetLatency * inputRate;

        if (Count > MaxLatency * inputRate)
        {
            Drain((int)target);
            Interlocked.Increment(ref _drains);
        }

        if (!_ready && Count >= target)
        {
            Array.Clear(_left);
            Array.Clear(_right);

            _fraction = 1.0;
            _lastLeft = _lastRight = 0;
            _ready    = true;
        }

        double error = Math.Clamp((Count - target) / target, -1.0, 1.0);
        double speed = GetProducerSpeed(inputRate);
        double ratio = speed < SlowSpeed && Count < target ? Math.Max(speed, 0.05) : 1.0;
        double step  = inputRate / SampleRate * ratio * (1.0 + error * MaxRateAdjust);

        for (int i = 0; i < destination.Length / Channels; i++)
        {
            if (_ready)
            {
                while (_fraction >= 1.0 && _ready)
                {
                    if (PullSample())
                    {
                        _fraction -= 1.0;
                    }
                    else
                    {
                        _ready = false;
                        Interlocked.Increment(ref _underruns);
                    }
                }

                if (_ready)
                {
                    _lastLeft   = ToPCM(Cubic(_left, (float)_fraction));
                    _lastRight  = ToPCM(Cubic(_right, (float)_fraction));
                    _fraction  += step;
                }
            }

            float targetGain = _ready ? 1f : 0f;

            if (_gain < targetGain)      _gain = Math.Min(_gain + GainStep, targetGain);
            else if (_gain > targetGain) _gain = Math.Max(_gain - GainStep, targetGain);

            destination[i * Channels]     = (short)(_lastLeft * _gain);
            destination[i * Channels + 1] = (short)(_lastRight * _gain);
        }
    }


    private double GetProducerSpeed(double inputRate)
    {
        long now = Stopwatch.GetTimestamp();
        double seconds = (double)(now - _speedStamp) / Stopwatch.Frequency;

        if (seconds >= SpeedWindow)
        {
            long written = Volatile.Read(ref _written);
            double speed = (written - _speedWritten) / (inputRate * seconds);

            _speed        += (speed - _speed) * 0.5;
            _speedStamp    = now;
            _speedWritten  = written;
        }

        return _speed;
    }

    private void Drain(int keep)
    {
        int head = _head;
        int excess = (Volatile.Read(ref _tail) - head) - keep;

        if (excess > 0)
            Volatile.Write(ref _head, head + excess);
    }

    private bool PullSample()
    {
        int head = _head;
        if (head == Volatile.Read(ref _tail))
            return false;

        uint sample = _ring[head & (MaxQueued - 1)];
        Volatile.Write(ref _head, head + 1);

        for (int i = 0; i < 3; i++)
        {
            _left[i]  = _left[i + 1];
            _right[i] = _right[i + 1];
        }

        _left[3]  = (short)sample         / 512f;
        _right[3] = (short)(sample >> 16) / 512f;
        return true;
    }

    private static float Cubic(float[] y, float t)
    {
        float a = -0.5f * y[0] + 1.5f * y[1] - 1.5f * y[2] + 0.5f * y[3];
        float b = y[0] - 2.5f * y[1] + 2.0f * y[2] - 0.5f * y[3];
        float c = -0.5f * y[0] + 0.5f * y[2];

        return ((a * t + b) * t + c) * t + y[1];
    }

    private static short ToPCM(float value) => (short)Math.Clamp(value * 0.80f * short.MaxValue, short.MinValue, short.MaxValue);
}