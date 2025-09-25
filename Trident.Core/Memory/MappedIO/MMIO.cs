using Trident.Core.Global;
using Trident.Core.Hardware.IO;
using Trident.Core.CPU.Pipeline;
using Trident.Core.Hardware.DMA;
using Trident.Core.Memory.Region;
using Trident.Core.Hardware.Graphics;
using System.Runtime.CompilerServices;
using Trident.Core.Hardware.Controller;
using Trident.Core.Hardware.Interrupts;


namespace Trident.Core.Memory.MappedIO
{
    internal partial class MMIO : IMemoryRegion
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


        private ushort Read(uint address)
        {
            if (!TryNormalize(address, out uint index))
                return 0; // TODO: return open bus

            return _registers[index].Read();
        }

        private void Write(uint address, ushort value)
        {
            if (!TryNormalize(address, out uint index))
                return;

            _registers[index].Write(value, WriteMask.Both);
        }

        #region External access
        public byte Read8(uint address, PipelineAccess access)
        {
            _step(1);

            if (!TryNormalize(address, out uint index))
                return 0; // TODO: return open bus

            int shift = (int)(address & 1) << 3;
            return (byte)(_registers[index].Read() >> shift);
        }

        public ushort Read16(uint address, PipelineAccess access)
        {
            _step(1);
            address = address.Align<ushort>();

            return Read(address);
        }

        public uint Read32(uint address, PipelineAccess access)
        {
            _step(1);
            address = address.Align<uint>();

            return (uint)(Read(address) | (Read(address | 2) << 16));
        }


        public void Write8(uint address, PipelineAccess access, byte value)
        {
            _step(1);

            if (!TryNormalize(address, out uint index))
                return;

            bool upper = (address & 1) != 0;
            WriteMask mask = upper ? WriteMask.Upper : WriteMask.Lower;

            ushort data = upper ? (ushort)(value << 8) : value;
            _registers[index].Write(data, mask);
        }

        public void Write16(uint address, PipelineAccess access, ushort value)
        {
            _step(1);
            Write(address.Align<ushort>(), value);
        }

        public void Write32(uint address, PipelineAccess access, uint value)
        {
            _step(1);
            address = address.Align<uint>();

            Write(address | 0, (ushort)(value));
            Write(address | 2, (ushort)(value >> 16));
        }


        public void Dispose() { }
        #endregion
    }


    [Flags]
    internal enum WriteMask : byte
    {
        None = 0,
        Lower = 1 << 0,
        Upper = 1 << 1,
        Both = Lower | Upper
    }

    internal static class WriteMaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsLower(this WriteMask mask) => (mask & WriteMask.Lower) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsUpper(this WriteMask mask) => (mask & WriteMask.Upper) != 0;
    }
}