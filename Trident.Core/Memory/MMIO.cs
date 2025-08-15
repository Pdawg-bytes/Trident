using System.Runtime.CompilerServices;
using Trident.Core.Global;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Hardware.IO;
using static Trident.Core.Hardware.IO.IORegisters;

namespace Trident.Core.Memory
{
    internal class MMIO(InterruptController irqController, HaltControl haltControl, PostFlag postFlag)
    {
        private readonly InterruptController _irqController = irqController;
        private readonly HaltControl _haltControl = haltControl;
        private readonly PostFlag _postFlag = postFlag;


        internal MemoryAccessHandler GetAccessHandler()
        {
            return new MemoryAccessHandler
            (
                read8:  (address, _) => Read8(address),
                read16: (address, _) => Read16(address.Align<ushort>()),
                read32: (address, _) => Read32(address.Align<uint>()),

                write8:  (address, _, value) => Write8(address, value),
                write16: (address, _, value) => Write16(address.Align<ushort>(), value),
                write32: (address, _, value) => Write32(address.Align<uint>(), value),

                dispose: null
            );
        }


        private byte Read8(uint address) => address switch
        {
            // Interrupt Controller registers
            IE + 0 or 
            IE + 1 or 
            IF + 0 or 
            IF + 1 or 
            IME 
                => _irqController.Read8(address),

            IME + 1 or 
            IME + 2 or 
            IME + 3 
                => 0,

            // System Control Registers
            POSTFLG => _postFlag.Read(),
            HALTCNT => 0,

            // TODO: return open bus
            _ => 0,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort Read16(uint address) => address switch
        {
            // Interrupt Controller registers
            IE or
            IF or
            IME
                => _irqController.Read16(address),

            _ => (ushort)((Read8(address + 1) << 8) | Read8(address))
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read32(uint address) => address switch
        {
            _ => (uint)
            (
                (Read8(address + 0) << 0) |
                (Read8(address + 1) << 8) |
                (Read8(address + 2) << 16) |
                (Read8(address + 3) << 24)
            )
        };


        private void Write8(uint address, byte value)
        {
            switch (address)
            {
                // Interrupt Controller registers
                case IE + 0:
                case IE + 1:
                case IF + 0:
                case IF + 1:
                case IME:
                    _irqController.Write8(address, value); break;

                // System Control Registers
                case POSTFLG: _postFlag.Write(value);    break;
                case HALTCNT: _haltControl.Write(value); break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write16(uint address, ushort value)
        {
            switch (address)
            {
                // Interrupt Controller registers
                case IE:
                case IF:
                case IME:
                    _irqController.Write16(address, value); break;

                default:
                    Write8(address + 0, (byte)(value >> 0));
                    Write8(address + 1, (byte)(value >> 8));
                    break;
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write32(uint address, uint value)
        {
            switch (address)
            {
                default:
                    Write8(address + 0, (byte)(value >> 0));
                    Write8(address + 1, (byte)(value >> 8));
                    Write8(address + 2, (byte)(value >> 16));
                    Write8(address + 3, (byte)(value >> 24));
                    break;
            }
        }
    }
}