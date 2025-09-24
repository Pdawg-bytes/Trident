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


        internal ushort ReadDMAControlH(uint id)
        {
            DMAChannel channel = _channels[id];
            return (ushort)
            (
                ((int)channel.DestinationControl  << 5)  | 
                ((int)channel.SourceControl       << 7)  |
                (channel.Repeat ?         (1 << 9) : 0)  |
                ((int)channel.TransferSize        << 10) |
                (channel.GamePakDRQ ?     (1 << 11) : 0) |
                ((int)channel.StartTiming         << 12) |
                (channel.InterruptOnEnd ? (1 << 14) : 0) |
                (channel.Enabled ?        (1 << 15) : 0)
            );
        }

        internal void WriteDMAControlH(ushort value, bool upper, bool lower, uint id)
        {
            DMAChannel channel = _channels[id];

            if (lower)
            {
                channel.DestinationControl = (AddressingMode)((value >> 5) & 0b11);
                channel.SourceControl      = (AddressingMode)((value >> 7) & 0b11);
            }

            if (upper)
            {
                channel.Repeat         = (value & (1 << 9))  != 0;
                channel.TransferSize   = (DMATransferSize)((value >> 10) & 1);
                channel.GamePakDRQ     = (value & (1 << 11)) != 0;
                channel.StartTiming    = (DMAStartTiming)((value >> 12) & 0b11);
                channel.InterruptOnEnd = (value & (1 << 14)) != 0;
                channel.Enabled        = (value & (1 << 15)) != 0;
            }
        }


        internal void WriteDMAControlL(ushort value, bool upper, bool lower, uint id)
        {
            DMAChannel channel = _channels[id];

            if (lower)
                channel.TransferLength = (ushort)((channel.TransferLength & 0xFF00) | value & 0x00FF);

            if (upper)
                channel.TransferLength = (ushort)((channel.TransferLength & 0x00FF) | value & 0xFF00);
        }


        internal void Reset()
        {
            for (uint i = 0; i < _channels.Length; i++)
                _channels[i] = new(i);
        }
    }
}