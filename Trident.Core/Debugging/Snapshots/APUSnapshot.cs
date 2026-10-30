namespace Trident.Core.Debugging.Snapshots;

public readonly struct APUSnapshot
(
    bool masterEnable,
    int sampleRate,
    int biasLevel,
    int psgVolume,
    int volumeLeft,
    int volumeRight,
    in APUSnapshot.FIFOSnapshot fifoA,
    in APUSnapshot.FIFOSnapshot fifoB,
    in APUSnapshot.PSGSnapshot  square1,
    in APUSnapshot.PSGSnapshot  square2,
    in APUSnapshot.PSGSnapshot  wave,
    in APUSnapshot.PSGSnapshot  noise)
{
    public readonly bool MasterEnable = masterEnable;
    public readonly int SampleRate    = sampleRate;
    public readonly int BiasLevel     = biasLevel;
    public readonly int PSGVolume     = psgVolume;
    public readonly int VolumeLeft    = volumeLeft;
    public readonly int VolumeRight   = volumeRight;

    public readonly FIFOSnapshot FIFOA = fifoA;
    public readonly FIFOSnapshot FIFOB = fifoB;

    public readonly PSGSnapshot Square1 = square1;
    public readonly PSGSnapshot Square2 = square2;
    public readonly PSGSnapshot Wave    = wave;
    public readonly PSGSnapshot Noise   = noise;

    public readonly struct FIFOSnapshot
    (
        bool enableLeft,
        bool enableRight,
        int timer,
        bool fullVolume,
        int count,
        sbyte sample)
    {
        public readonly bool EnableLeft  = enableLeft;
        public readonly bool EnableRight = enableRight;
        public readonly int Timer        = timer;
        public readonly bool FullVolume  = fullVolume;
        public readonly int Count        = count;
        public readonly sbyte Sample     = sample;
    }

    public readonly struct PSGSnapshot
    (
        bool active,
        bool enableLeft,
        bool enableRight,
        bool lengthEnabled,
        int level)
    {
        public readonly bool Active          = active;
        public readonly bool EnableLeft      = enableLeft;
        public readonly bool EnableRight     = enableRight;
        public readonly bool LengthEnabled   = lengthEnabled;
        public readonly int Level            = level;
    }
}