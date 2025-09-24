using Trident.Core.Hardware.IO;
using Trident.Core.Hardware.DMA;
using Trident.Core.Hardware.Graphics;
using System.Runtime.CompilerServices;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Hardware.Controller;

namespace Trident.Core.Memory.MappedIO
{
    internal partial class MMIO
    {
        // Since all but two MMIO registers are 2 or 4 bytes in size - meaning they are all accessed on half-word boundaries - 
        // we can normalize the addresses to those boundaries, halving the required space needed to cover the MMIO map.
        // ---
        // The last MMIO register on a regular GBA is POSTFLG, at 0x300. We combine POSTFLG and HALTCNT to include those two
        // registers, therefore, our normalized address space can be exactly half that (+ 1 to include 0x300).
        private const int REGISTER_COUNT = 0x181;
        private readonly RegisterAccessor[] _registers = new RegisterAccessor[REGISTER_COUNT];

        private readonly Action<uint> _step;

        private readonly PPURegisters _ppuRegisters;

        private readonly DMAManager _dmaManager;

        private readonly Keypad _keypad;

        private readonly InterruptController _irqController;

        private readonly WaitControl _waitControl;
        private readonly PostHalt _postHalt;

        internal MMIO
        (
            Action<uint> step,

            PPURegisters ppuRegisters,

            DMAManager dmaManager,

            Keypad keypad,

            InterruptController irqController,

            WaitControl waitControl,
            PostHalt postHalt
        )
        {
            _step = step;

            _ppuRegisters = ppuRegisters;

            _dmaManager = dmaManager;

            _keypad = keypad;

            _irqController = irqController;

            _waitControl = waitControl;
            _postHalt = postHalt;

            InitializeRegisterMap();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryNormalize(uint address, out uint index)
        {
            index = (address - 0x04000000) >> 1;

            if (index >= REGISTER_COUNT)
                return false;

            return true;
        }


        private byte Read8(uint address)
        {
            if (!TryNormalize(address, out uint index))
                return 0; // TODO: return open bus

            int shift = (int)(address & 1) << 3;
            return (byte)(_registers[index].Read() >> shift);
        }

        private ushort Read16(uint address)
        {
            if (!TryNormalize(address, out uint index))
                return 0; // TODO: return open bus

            return _registers[index].Read();
        }

        private uint Read32(uint address) => (uint)(Read16(address) | (Read16(address | 2) << 16));


        private void Write8(uint address, byte value)
        {
            if (!TryNormalize(address, out uint index))
                return;

            bool upper = (address & 1) != 0;

            ushort data = upper ? (ushort)(value << 8) : value;

            // We're either writing the upper or lower byte; never both at once.
            _registers[index].Write(data, upper, !upper);
        }

        private void Write16(uint address, ushort value)
        {
            if (!TryNormalize(address, out uint index))
                return;

            _registers[index].Write(value, true, true);
        }
    }
}