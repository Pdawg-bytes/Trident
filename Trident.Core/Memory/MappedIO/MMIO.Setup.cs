using Trident.Core.Hardware.Graphics;
using static Trident.Core.Hardware.IO.IORegisters;

namespace Trident.Core.Memory.MappedIO;

internal partial class MMIO
{
    private readonly Func<ushort> _zeroRead = () => 0;
    private readonly Action<ushort, WriteMask> _emptyWrite = (_, _) => { };

    private ushort _rcnt;

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
        // TODO: PPU read-only registers will return open bus on read, NOT 0.
        SetAccessor(DISPCNT,   _ppu.DisplayControl.Read,  _ppu.DisplayControl.Write);
        SetAccessor(GREENSWAP, _ppu.GreenSwap.Read,       _ppu.GreenSwap.Write);
        SetAccessor(DISPSTAT,  _ppu.DisplayStatus.Read,   _ppu.DisplayStatus.Write);
        SetAccessor(VCOUNT,    () => (ushort)_ppu.VCount, _emptyWrite);

        RegisterBGBase();
        RegisterAffineBG(2, BG2PA, BG2X, BG2Y);
        RegisterAffineBG(3, BG3PA, BG3X, BG3Y);

        RegisterWindows();

        SetAccessor(MOSAIC,   _zeroRead,              _ppu.Mosaic.Write);
        SetAccessor(BLDCNT,   _ppu.Blend.ReadControl, _ppu.Blend.WriteControl);
        SetAccessor(BLDALPHA, _zeroRead,              _ppu.Blend.WriteAlpha);
        SetAccessor(BLDY,     _zeroRead,              _ppu.Blend.WriteBrightness);


        // Audio registers
        SetAccessor(SOUND1CNT_L, _apu.Square1.ReadSweep,     _apu.Square1.WriteSweep);
        SetAccessor(SOUND1CNT_H, _apu.Square1.ReadControl,   _apu.Square1.WriteControl);
        SetAccessor(SOUND1CNT_X, _apu.Square1.ReadFrequency, _apu.Square1.WriteFrequency);

        SetAccessor(SOUND2CNT_L, _apu.Square2.ReadControl,   _apu.Square2.WriteControl);
        SetAccessor(SOUND2CNT_H, _apu.Square2.ReadFrequency, _apu.Square2.WriteFrequency);

        SetAccessor(SOUND3CNT_L, _apu.Wave.ReadControl,      _apu.Wave.WriteControl);
        SetAccessor(SOUND3CNT_H, _apu.Wave.ReadVolume,       _apu.Wave.WriteVolume);
        SetAccessor(SOUND3CNT_X, _apu.Wave.ReadFrequency,    _apu.Wave.WriteFrequency);

        SetAccessor(SOUND4CNT_L, _apu.Noise.ReadControl,     _apu.Noise.WriteControl);
        SetAccessor(SOUND4CNT_H, _apu.Noise.ReadFrequency,   _apu.Noise.WriteFrequency);

        for (int i = 0; i < 8; i++)
        {
            int index = i;
            SetAccessor(WAVE_RAM + (uint)(i * 2), () => _apu.Wave.ReadWaveRAM(index), (v, m) => _apu.Wave.WriteWaveRAM(index, v, m));
        }

        SetAccessor(SOUNDCNT_L, _apu.ReadControlL, _apu.WriteControlL);
        SetAccessor(SOUNDCNT_H, _apu.ReadControlH, _apu.WriteControlH);
        SetAccessor(SOUNDCNT_X, _apu.ReadControlX, _apu.WriteControlX);
        SetAccessor(SOUNDBIAS,  _apu.ReadBias,     _apu.WriteBias);

        SetAccessor(FIFO_A + 0, _zeroRead, (v, m) => _apu.WriteFIFO(0, v, m));
        SetAccessor(FIFO_A + 2, _zeroRead, (v, m) => _apu.WriteFIFO(0, v, m));
        SetAccessor(FIFO_B + 0, _zeroRead, (v, m) => _apu.WriteFIFO(1, v, m));
        SetAccessor(FIFO_B + 2, _zeroRead, (v, m) => _apu.WriteFIFO(1, v, m));


        // DMA registers
        RegisterDMAChannel(0, DMA0SAD, DMA0DAD, DMA0CNT_L, DMA0CNT_H);
        RegisterDMAChannel(1, DMA1SAD, DMA1DAD, DMA1CNT_L, DMA1CNT_H);
        RegisterDMAChannel(2, DMA2SAD, DMA2DAD, DMA2CNT_L, DMA2CNT_H);
        RegisterDMAChannel(3, DMA3SAD, DMA3DAD, DMA3CNT_L, DMA3CNT_H);


        // Timer registers
        RegisterTimerChannel(0, TM0CNT_L, TM0CNT_H);
        RegisterTimerChannel(1, TM1CNT_L, TM1CNT_H);
        RegisterTimerChannel(2, TM2CNT_L, TM2CNT_H);
        RegisterTimerChannel(3, TM3CNT_L, TM3CNT_H);


        // Keypad registers
        SetAccessor(KEYINPUT, _keypad.ReadKeyInput, _emptyWrite);
        SetAccessor(KEYCNT, _keypad.ReadKeyControl, _keypad.WriteKeyControl);


        // TEMP
        SetAccessor
        (
            RCNT,
            () => _rcnt,
            (value, mask) =>
            {
                _rcnt = MMIO.Merge(_rcnt, value, mask);
            }
        );


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


    private void RegisterTimerChannel(int id, uint cntL, uint cntH)
    {
        int localId = id;
        SetAccessor(cntL, () => _timerManager.ReadCounterReload(localId), (v, m) => _timerManager.WriteReload(localId, v, m));
        SetAccessor(cntH, () => _timerManager.ReadControl(localId),       (v, m) => _timerManager.WriteControl(localId, v, m));
    }


    private void RegisterDMAChannel(uint id, uint sadBase, uint dadBase, uint cntL, uint cntH)
    {
        // DMAXCNT
        SetAccessor(cntL, _zeroRead,                            (v, m) => _dmaManager.WriteDMAControlL(v, m, id));
        SetAccessor(cntH, () => _dmaManager.ReadDMAControl(id), (v, m) => _dmaManager.WriteDMAControlH(v, m, id));

        // DMAXSAD
        SetAccessor(sadBase + 0, _zeroRead, (v, m) => _dmaManager.WriteDMATarget(v, WriteMask.Lower, m, id, source: true));
        SetAccessor(sadBase + 2, _zeroRead, (v, m) => _dmaManager.WriteDMATarget(v, WriteMask.Upper, m, id, source: true));

        // DMAXDAD
        SetAccessor(dadBase + 0, _zeroRead, (v, m) => _dmaManager.WriteDMATarget(v, WriteMask.Lower, m, id, source: false));
        SetAccessor(dadBase + 2, _zeroRead, (v, m) => _dmaManager.WriteDMATarget(v, WriteMask.Upper, m, id, source: false));
    }


    private void RegisterBGBase()
    {
        for (uint id = 0; id < 4; id++)
        {
            Background bg = _ppu.Backgrounds[id];

            SetAccessor(BG0CNT  + id * 2, bg.ReadControl, bg.WriteControl);
            SetAccessor(BG0HOFS + id * 4, _zeroRead,      (v, m) => bg.WriteOffset(false, v, m));
            SetAccessor(BG0VOFS + id * 4, _zeroRead,      (v, m) => bg.WriteOffset(true,  v, m));
        }
    }

    private void RegisterAffineBG(uint id, uint basePA, uint baseX, uint baseY)
    {
        Background bg = _ppu.Backgrounds[id];

        SetAccessor(basePA + 0, _zeroRead, (v, m) => bg.WriteAffineParameter(AffineParameter.A, v, m));
        SetAccessor(basePA + 2, _zeroRead, (v, m) => bg.WriteAffineParameter(AffineParameter.B, v, m));
        SetAccessor(basePA + 4, _zeroRead, (v, m) => bg.WriteAffineParameter(AffineParameter.C, v, m));
        SetAccessor(basePA + 6, _zeroRead, (v, m) => bg.WriteAffineParameter(AffineParameter.D, v, m));

        SetAccessor(baseX + 0, _zeroRead, (v, m) => bg.WriteReference(false, v, WriteMask.Lower, m));
        SetAccessor(baseX + 2, _zeroRead, (v, m) => bg.WriteReference(false, v, WriteMask.Upper, m));

        SetAccessor(baseY + 0, _zeroRead, (v, m) => bg.WriteReference(true, v, WriteMask.Lower, m));
        SetAccessor(baseY + 2, _zeroRead, (v, m) => bg.WriteReference(true, v, WriteMask.Upper, m));
    }

    private void RegisterWindows()
    {
        for (uint id = 0; id < 2; id++)
        {
            Window win = _ppu.Windows[id];

            SetAccessor(WIN0H + id * 2, () => win.ReadBounds(false), (v, m) => win.WriteBounds(false, v, m));
            SetAccessor(WIN0V + id * 2, () => win.ReadBounds(true),  (v, m) => win.WriteBounds(true,  v, m));
        }

        SetAccessor(WININ,  _ppu.WindowInside.Read,  _ppu.WindowInside.Write);
        SetAccessor(WINOUT, _ppu.WindowOutside.Read, _ppu.WindowOutside.Write);
    }
}