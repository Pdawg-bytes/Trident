using Trident.Core.Bus;
using Trident.Core.Scheduling;
using Trident.Core.Hardware.Interrupts;

namespace Trident.Core.Hardware.DMA
{
    internal class DMAManager
    {
        private GBABusView _busView;
        private readonly Action<InterruptSource, int> _raiseIRQ;
        private readonly Scheduler _scheduler;

        private readonly DMAChannel[] _channels = new DMAChannel[4];

        internal DMAManager(Action<InterruptSource, int> raiseIRQ, Scheduler scheduler)
        {
            _raiseIRQ = raiseIRQ;
            _scheduler = scheduler;

            Reset();
        }

        internal void SetBusView(GBABusView busView) => _busView = busView;


        internal void Reset()
        {
            for (uint i = 0; i < _channels.Length; i++)
                _channels[i] = new(i);
        }
    }
}