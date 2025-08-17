using Trident.Core.Global;
using Trident.Core.Hardware.IO;
using System.Runtime.CompilerServices;
using Trident.Core.Hardware.Interrupts;
using Trident.Core.Hardware.Controller;

using static Trident.Core.Hardware.IO.IORegisters;

namespace Trident.Core.Memory
{
    internal class MMIO
    (
        Action<uint> step,

        Keypad keypad,

        InterruptController irqController, 

        WaitControl waitControl, 
        PostFlag postFlag, 
        HaltControl haltControl
    )
    {
        private readonly Action<uint> _step = step;

        private readonly Keypad _keypad = keypad;

        private readonly InterruptController _irqController = irqController;

        private readonly WaitControl _waitControl = waitControl;
        private readonly PostFlag _postFlag = postFlag;
        private readonly HaltControl _haltControl = haltControl;


        // TODO: possibly service regs primarily via 16-bit accesses?
        internal MemoryAccessHandler GetAccessHandler() => new
        (
            read8:  (address, _) => { _step(1); return Read8(address); },
            read16: (address, _) => { _step(1); return Read16(address.Align<ushort>()); },
            read32: (address, _) => { _step(1); return Read32(address.Align<uint>()); },

            write8:  (address, _, value) => { _step(1); Write8(address, value); },
            write16: (address, _, value) => { _step(1); Write16(address.Align<ushort>(), value); },
            write32: (address, _, value) => { _step(1); Write32(address.Align<uint>(), value); },

            dispose: null
        );


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
        }
    }
}