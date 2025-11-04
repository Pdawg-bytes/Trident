namespace Trident.Core.Debugging.Snapshots
{
    public readonly struct IRQSnapshot(bool globalInterruptEnable, ushort interruptEnable, ushort interruptFlag)
    {
        public readonly bool GlobalInterruptEnable = globalInterruptEnable;
        public readonly ushort InterruptEnable = interruptEnable;
        public readonly ushort InterruptFlag = interruptFlag;
    }
}