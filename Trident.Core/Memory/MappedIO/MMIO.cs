using Trident.Core.Global;
using Trident.Core.Hardware.IO;
using System.Runtime.CompilerServices;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Hardware.Controller;

using static Trident.Core.Hardware.IO.IORegisters;

namespace Trident.Core.Memory.MappedIO
{
    internal partial class MMIO
    {
        // Since all but two MMIO registers are 2 or 4 bytes in length - meaning they are all accessed on half-word boundaries - 
        // we can normalize the addresses to those boundaries, halving the required space needed to cover the MMIO map.
        // ---
        // The last MMIO register on a regular GBA is POSTFLG, at 0x300. We combine POSTFLG and HALTCNT to include those two
        // registers, therefore, our normalized address space can be exactly 0x181 wide.
        private const int REGISTER_COUNT = 0x181;
        private readonly RegisterAccessor[] _registers = new RegisterAccessor[REGISTER_COUNT];

        private readonly Action<uint> _step;

        private readonly Keypad _keypad;

        private readonly InterruptController _irqController;

        private readonly WaitControl _waitControl;
        private readonly PostFlag _postFlag;
        private readonly HaltControl _haltControl;

        internal MMIO
        (
            Action<uint> step,

            Keypad keypad,

            InterruptController irqController,

            WaitControl waitControl,
            PostFlag postFlag,
            HaltControl haltControl
        )
        {
            _step = step;

            _keypad = keypad;

            _irqController = irqController;

            _waitControl = waitControl;
            _postFlag = postFlag;
            _haltControl = haltControl;

            InitializeRegisterMap();
        }

        private bool TryNormalize(uint address, out uint index)
        {
            if (address > 0x4000300)
            {
                index = uint.MaxValue;
                return false;
            }

            index = (address & 0x03FF) >> 1;
            return true;
        }

        private byte Read8(uint address)
        {
            // Align the address to a half-word boundary and normalize it
            uint index = address.Align<ushort>() >> 1;

            int shift = (int)(address & 1) << 3;
            return (byte)(_registers[index].Read(address) >> shift);
        }

        /*
        private byte Read8(uint address) => address switch
        {
            // Keypad registers
            KEYINPUT + 0 => _keypad.ReadKeyInput8(upper: false),
            KEYINPUT + 1 => _keypad.ReadKeyInput8(upper: true),

            KEYCNT + 0 => _keypad.ReadKeyControl8(upper: false),
            KEYCNT + 1 => _keypad.ReadKeyControl8(upper: true),

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
            WAITCNT + 0 => _waitControl.ReadLower(),
            WAITCNT + 1 => _waitControl.ReadUpper(),
            WAITCNT + 2 or
            WAITCNT + 3
                => 0,

            POSTFLG => _postFlag.Read(),
            HALTCNT => 0,

            // TODO: return open bus
            _ => 0,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort Read16(uint address) => address switch
        {
            // Keypad registers
            KEYINPUT => _keypad.ReadKeyInput16(),
            KEYCNT => _keypad.ReadKeyControl16(),

            // Interrupt Controller registers
            IE or
            IF or
            IME
                => _irqController.Read16(address),

            _ => (ushort)(Read8(address + 1) << 8 | Read8(address))
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read32(uint address) => address switch
        {
            _ => (uint)
            (
                Read8(address + 0) << 0  |
                Read8(address + 1) << 8  |
                Read8(address + 2) << 16 |
                Read8(address + 3) << 24
            )
        };


        private void Write8(uint address, byte value)
        {
            switch (address)
            {
                // Keypad registers
                case KEYCNT + 0: _keypad.WriteKeyControl8(upper: false, value); break;
                case KEYCNT + 1: _keypad.WriteKeyControl8(upper: true, value);  break;

                // Interrupt Controller registers
                case IE + 0:
                case IE + 1:
                case IF + 0:
                case IF + 1:
                case IME:
                    _irqController.Write8(address, value); break;

                // System Control Registers
                case WAITCNT + 0: _waitControl.WriteLower(value); break;
                case WAITCNT + 1: _waitControl.WriteUpper(value); break;
                case POSTFLG: _postFlag.Write(value);    break;
                case HALTCNT: _haltControl.Write(value); break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write16(uint address, ushort value)
        {
            switch (address)
            {
                // Keypad registers
                case KEYCNT: _keypad.WriteKeyControl16(value); break;

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
        }*/
    }
}