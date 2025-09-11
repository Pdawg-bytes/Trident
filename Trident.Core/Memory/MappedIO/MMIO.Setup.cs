using Trident.Core.Global;
using Trident.Core.Hardware.IO;
using Trident.Core.Hardware.Graphics.Registers;

using static Trident.Core.Hardware.IO.IORegisters;

namespace Trident.Core.Memory.MappedIO
{
    internal partial class MMIO
    {
        private readonly Func<ushort> _zeroRead = () => 0;
        private readonly Action<ushort, bool, bool> _emptyWrite = (_, _, _) => { };

        private void InitializeRegisterMap()
        {
            RegisterAccessor openBusRegister = new 
            (
                read: () => 0, // TODO: return open bus
                write: _emptyWrite
            );

            // There are some 'holes' - addresses not mapped to concrete registers or 0.
            for (int i = 0; i < REGISTER_COUNT; i++)
                _registers[i] = openBusRegister;


            // PPU registers
            SetAccessor(DISPCNT, _ppuRegisters.DisplayControl.Read, _ppuRegisters.DisplayControl.Write);
            SetAccessor(GREENSWAP, () => (ushort)_ppuRegisters.Greenswap, (value, upper, lower) => { if (lower) _ppuRegisters.Greenswap = value & 1u; });
            SetAccessor(DISPSTAT, _ppuRegisters.DisplayStatus.Read, _ppuRegisters.DisplayStatus.Write);
            SetAccessor(VCOUNT, () => (ushort)_ppuRegisters.VCount, _emptyWrite);

            BackgroundControl bgxcnt = _ppuRegisters.BackgroundControls[0];
            SetAccessor(BG0CNT, bgxcnt.Read, bgxcnt.Write);
            bgxcnt = _ppuRegisters.BackgroundControls[1];
            SetAccessor(BG1CNT, bgxcnt.Read, bgxcnt.Write);
            bgxcnt = _ppuRegisters.BackgroundControls[2];
            SetAccessor(BG2CNT, bgxcnt.Read, bgxcnt.Write);
            bgxcnt = _ppuRegisters.BackgroundControls[3];
            SetAccessor(BG3CNT, bgxcnt.Read, bgxcnt.Write);

            
            // Keypad registers
            SetAccessor(KEYINPUT, _keypad.ReadKeyInput, _emptyWrite);
            SetAccessor(KEYCNT, _keypad.ReadKeyControl, _keypad.WriteKeyControl);

            // Interrupt Controller registers
            SetAccessor(IE, _irqController.ReadIE, _irqController.WriteIE);
            SetAccessor(IF, _irqController.ReadIF, _irqController.WriteIF);
            SetAccessor(IME, _irqController.ReadIME, _irqController.WriteIME);
            MapUnusedRegister(IME + 2);

            // System Control registers
            SetAccessor(WAITCNT, _waitControl.Read, _waitControl.Write);
            MapUnusedRegister(WAITCNT + 2);

            SetAccessor(POSTFLG, _postHalt.Read, _postHalt.Write);
        }

        private void SetAccessor(uint register, Func<ushort> read, Action<ushort, bool, bool> write)
        {
            if (!TryNormalize(register, out uint index))
                throw new ArgumentOutOfRangeException(nameof(register), $"MMIO: Tried to initialize invalid MMIO register: 0x{register:X}");

            _registers[index] = new RegisterAccessor(read, write);
        }

        private void MapUnusedRegister(uint register)
        {
            if (!TryNormalize(register, out uint index))
                throw new ArgumentOutOfRangeException(nameof(register), $"MMIO: Tried to map invalid unused MMIO register: 0x{register:X}");

            _registers[index] = new RegisterAccessor(_zeroRead, _emptyWrite);
        }


        internal MemoryAccessHandler GetAccessHandler() => new
        (
            read8:  (address, _) => { _step(1); return Read8(address);                  },
            read16: (address, _) => { _step(1); return Read16(address.Align<ushort>()); },
            read32: (address, _) => { _step(1); return Read32(address.Align<uint>());   },

            write8:  (address, _, value) => { _step(1); Write8(address, value);                  },
            write16: (address, _, value) => { _step(1); Write16(address.Align<ushort>(), value); },
            write32: (address, _, value) => { _step(1); Write32(address.Align<uint>(), value);   },

            dispose: null
        );
    }
}