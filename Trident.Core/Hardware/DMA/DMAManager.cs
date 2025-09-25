using Trident.Core.Bus;
using Trident.Core.Scheduling;
using Trident.Core.Memory.MappedIO;
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



        internal ushort ReadDMAControl(uint id)
        {
            DMAChannel channel = _channels[id];
            return (ushort)
            (
                ((int)channel.DestinationControl  <<  5) | 
                ((int)channel.SourceControl       <<  7) |
                (channel.Repeat ?         (1 << 9)  : 0) |
                ((int)channel.TransferSize        << 10) |
                (channel.GamePakDRQ ?     (1 << 11) : 0) |
                ((int)channel.StartTiming         << 12) |
                (channel.InterruptOnEnd ? (1 << 14) : 0) |
                (channel.Enabled ?        (1 << 15) : 0)
            );
        }

        internal void WriteDMAControlH(ushort value, WriteMask mask, uint id)
        {
            DMAChannel channel = _channels[id];

            if (mask.IsLower())
            {
                channel.DestinationControl = (AddressingMode)((value >> 5) & 0b11);
                channel.SourceControl      = (AddressingMode)((value >> 7) & 0b11);
            }

            if (mask.IsUpper())
            {
                channel.Repeat         = (value & (1 << 9))  != 0;
                channel.TransferSize   = (DMATransferSize)((value >> 10) & 1);
                channel.GamePakDRQ     = (value & (1 << 11)) != 0;
                channel.StartTiming    = (DMAStartTiming)((value >> 12) & 0b11);
                channel.InterruptOnEnd = (value & (1 << 14)) != 0;
                channel.Enabled        = (value & (1 << 15)) != 0;
            }
        }

        internal void WriteDMAControlL(ushort value, WriteMask mask, uint id)
        {
            DMAChannel channel = _channels[id];

            if (mask.IsLower())
                channel.TransferLength = (ushort)((channel.TransferLength & 0xFF00) | value & 0x00FF);

            if (mask.IsUpper())
                channel.TransferLength = (ushort)((channel.TransferLength & 0x00FF) | value & 0xFF00);
        }


        internal void WriteDMATarget(ushort value, WriteMask wordMask, WriteMask byteMask, uint id, bool source)
        {
            ref uint address = ref (source ? ref _channels[id].Source : ref _channels[id].Destination);

            uint lo = (uint)(value & 0x00FF);
            uint hi = (uint)(value & 0xFF00);

            switch (wordMask)
            {
                case WriteMask.Lower:
                    if (byteMask.IsLower())
                        address = (address & ~0x000000FFu) | lo;
                    if (byteMask.IsUpper())
                        address = (address & ~0x0000FF00u) | hi;
                    break;

                case WriteMask.Upper:
                    if (byteMask.IsLower())
                        address = (address & ~0x00FF0000u) | (lo << 16);
                    if (byteMask.IsUpper())
                        address = (address & ~0xFF000000u) | (hi << 16);
                    break;
            }
        }


        internal void Reset()
        {
            for (uint i = 0; i < _channels.Length; i++)
                _channels[i] = new(i);
        }
    }
}