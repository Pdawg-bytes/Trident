using Trident.Core.Hardware.IO;
using Trident.Core.Hardware.Graphics.Registers;
using static Trident.Core.Hardware.IO.IORegisters;

namespace Trident.Core.Memory.MappedIO
{
    internal partial class MMIO
    {
        private readonly Func<ushort> _zeroRead = () => 0;
        private readonly Action<ushort, WriteMask> _emptyWrite = (_, _) => { };

        private void InitializeRegisterMap()
        {
            RegisterAccessor openBusRegister = new 
            (
                read: () => 0, // TODO: return open bus
                write: _emptyWrite
            );

            // There are some 'holes' - addresses not mapped to concrete registers or 0.
            for (int i = 0; i < RegisterCount; i++)
                _registers[i] = openBusRegister;


            // PPU registers
            SetAccessor(DISPCNT, _ppuRegisters.DisplayControl.Read, _ppuRegisters.DisplayControl.Write);
            SetAccessor(GREENSWAP, () => (ushort)_ppuRegisters.Greenswap, (value, mask) => { if (mask.IsLower()) _ppuRegisters.Greenswap = value & 1u; });
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


            // DMA registers
            RegisterDMAChannel(0, DMA0SAD, DMA0DAD, DMA0CNT_L, DMA0CNT_H);
            RegisterDMAChannel(1, DMA1SAD, DMA1DAD, DMA1CNT_L, DMA1CNT_H);
            RegisterDMAChannel(2, DMA2SAD, DMA2DAD, DMA2CNT_L, DMA2CNT_H);
            RegisterDMAChannel(3, DMA3SAD, DMA3DAD, DMA3CNT_L, DMA3CNT_H);


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

        private void SetAccessor(uint register, Func<ushort> read, Action<ushort, WriteMask> write)
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


        private void RegisterDMAChannel(uint id, uint sadBase, uint dadBase, uint cntL, uint cntH)
        {
            // DMAXCNT
            SetAccessor(cntL, _zeroRead,                            (value, mask) => _dmaManager.WriteDMAControlL(value, mask, id));
            SetAccessor(cntH, () => _dmaManager.ReadDMAControl(id), (value, mask) => _dmaManager.WriteDMAControlH(value, mask, id));

            // DMAXSAD
            SetAccessor(sadBase + 0, _zeroRead, (value, mask) => _dmaManager.WriteDMATarget(value, WriteMask.Lower, mask, id, true));
            SetAccessor(sadBase + 2, _zeroRead, (value, mask) => _dmaManager.WriteDMATarget(value, WriteMask.Upper, mask, id, true));

            // DMAXDAD
            SetAccessor(dadBase + 0, _zeroRead, (value, mask) => _dmaManager.WriteDMATarget(value, WriteMask.Lower, mask, id, false));
            SetAccessor(dadBase + 2, _zeroRead, (value, mask) => _dmaManager.WriteDMATarget(value, WriteMask.Upper, mask, id, false));
        }
    }
}