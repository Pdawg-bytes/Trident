using OpenTK.Audio.OpenAL;
using Trident.Core.Hardware.Audio;

namespace Trident.Audio;

internal sealed class AudioOutput : IDisposable
{
    private const int BufferCount     = 4;
    private const int WakeTimeoutMs   = 8;
    private const int FramesPerBuffer = 512;

    private readonly AudioStream _source;
    private readonly short[] _scratch = new short[FramesPerBuffer * AudioStream.Channels];

    private ALDevice _device;
    private ALContext _context;
    private int _alSource;
    private int[] _buffers            = [];
    private readonly Queue<int> _free = new(BufferCount);

    private readonly int[] _unqueued = new int[BufferCount];
    private readonly int[] _one      = new int[1];

    private Thread? _thread;
    private volatile bool _running;

    internal bool IsAvailable { get; private set; }

    internal AudioOutput(AudioStream source) => _source = source;

    internal void Start()
    {
        try
        {
            _device = ALC.OpenDevice(null);
            if (_device == ALDevice.Null)
                return;

            _context = ALC.CreateContext(_device, (int[]?)null);
            ALC.MakeContextCurrent(_context);

            _alSource = AL.GenSource();
            _buffers  = AL.GenBuffers(BufferCount);

            foreach (int buffer in _buffers)
                _free.Enqueue(buffer);

            IsAvailable = true;
        }
        catch (Exception)
        {
            Shutdown();
            return;
        }

        _running = true;
        _thread = new Thread(Pump) { Name = "Audio Output", IsBackground = true };
        _thread.Start();
    }

    private void Pump()
    {
        while (_running)
        {
            _source.Wait(WakeTimeoutMs);

            AL.GetSource(_alSource, ALGetSourcei.BuffersProcessed, out int processed);
            if (processed > 0)
            {
                AL.SourceUnqueueBuffers(_alSource, processed, _unqueued);
                for (int i = 0; i < processed; i++)
                    _free.Enqueue(_unqueued[i]);
            }

            while (_free.Count > 0)
            {
                _source.Read(_scratch);

                int buffer = _free.Dequeue();
                AL.BufferData(buffer, ALFormat.Stereo16, _scratch, _source.SampleRate);
                _one[0] = buffer;
                AL.SourceQueueBuffers(_alSource, 1, _one);
            }

            AL.GetSource(_alSource, ALGetSourcei.BuffersQueued, out int queued);
            AL.GetSource(_alSource, ALGetSourcei.SourceState, out int state);
            if (queued > 0 && (ALSourceState)state != ALSourceState.Playing)
                AL.SourcePlay(_alSource);
        }
    }

    private void Shutdown()
    {
        if (_alSource != 0)
        {
            AL.SourceStop(_alSource);
            AL.DeleteSource(_alSource);
            _alSource = 0;
        }

        if (_buffers.Length > 0)
        {
            AL.DeleteBuffers(_buffers);
            _buffers = [];
        }

        if (_context != ALContext.Null)
        {
            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(_context);
            _context = ALContext.Null;
        }

        if (_device != ALDevice.Null)
        {
            ALC.CloseDevice(_device);
            _device = ALDevice.Null;
        }

        IsAvailable = false;
    }

    public void Dispose()
    {
        _running = false;
        _thread?.Join();
        _thread = null;
        Shutdown();
    }
}