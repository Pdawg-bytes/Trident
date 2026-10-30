namespace Trident.Core.Hardware.Audio.PSG;

internal abstract class PSGChannel
{
    internal bool Active { get; private protected set; }

    protected bool LengthEnabled;
    protected int  LengthCounter;

    internal bool LengthActive    => LengthEnabled;
    internal int  LengthRemaining => LengthCounter;

    protected abstract int MaxLength { get; }

    internal abstract int Output { get; }
    internal abstract void Tick(int cycles);

    internal virtual void ClockEnvelope() { }

    internal void ClockLength()
    {
        if (!LengthEnabled || LengthCounter == 0)
            return;

        if (--LengthCounter == 0)
            Active = false;
    }

    internal virtual void Reset()
    {
        Active        = false;
        LengthEnabled = false;
        LengthCounter = 0;
    }

    protected void LoadLength(int value) => LengthCounter = MaxLength - value;

    protected void ReloadEmptyLength()
    {
        if (LengthCounter == 0)
            LengthCounter = MaxLength;
    }
}