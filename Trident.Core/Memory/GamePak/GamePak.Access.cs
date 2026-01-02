using Trident.Core.CPU.Pipeline;
using Trident.Core.Memory.Region;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.GamePak;

internal partial class GamePak
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WaitAccess16(uint address, bool seqAccess)
    {
        uint region = (address >> 25) & 0b11;
        _step(_waitControl.AccessTimings16[seqAccess ? 1 : 0][region]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WaitAccess32(uint address, bool seqAccess)
    {
        uint region = (address >> 25) & 0b11;
        _step(_waitControl.AccessTimings32[seqAccess ? 1 : 0][region]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSequential(uint address, PipelineAccess access)
    {
        bool markedSequential = (access & PipelineAccess.Sequential) != 0;
        bool boundaryAligned = (address & 0x1FFFF) == 0;
        bool dmaTransition = ((int)access & (int)PipelineAccess.DMA) != 0;

        // GBAtek: Non-sequential timing is used at each 128KB boundary of the ROM.
        // Entering or exiting a DMA also means that the access is non-sequential.
        //             ^ TODO
        return markedSequential && !boundaryAligned && !dmaTransition;
    }


    internal byte Read8<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess =>
        (byte)(Read16<TAccess>(address, access) >> ((int)(address & 1) << 3));

    internal ushort Read16<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
    {
        bool isSequential = IsSequential(address, access);

        WaitAccess16(address, isSequential);
        return ReadData16<TAccess>(address, isSequential);
    }

    internal uint Read32<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
    {
        bool isSequential = IsSequential(address, access);

        WaitAccess32(address, isSequential);
        return ReadData32<TAccess>(address, isSequential);
    }


    internal void Write8<TAccess>(uint address, PipelineAccess access, byte value) where TAccess : struct, IAccess =>
        Write16<TAccess>(address, access, (ushort)(value * 0x0101));

    internal void Write16<TAccess>(uint address, PipelineAccess access, ushort value) where TAccess : struct, IAccess
    {
        bool isSequential = IsSequential(address, access);

        WaitAccess16(address, isSequential);
        WriteData16<TAccess>(address, isSequential, value);
    }

    internal void Write32<TAccess>(uint address, PipelineAccess access, uint value) where TAccess : struct, IAccess
    {
        bool isSequential = IsSequential(address, access);

        WaitAccess32(address, isSequential);
        WriteData16<TAccess>(address | 0, isSequential, (ushort)value);
        WriteData16<TAccess>(address | 2, isSequential, (ushort)(value >> 16));
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte ReadBackup(uint address)
    {
        _step(_waitControl.AccessTimings16[0][3]);
        return _backupDevice.Read(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteBackup(uint address, byte value)
    {
        _step(_waitControl.AccessTimings16[0][3]);
        _backupDevice.Write(address & 0x0EFFFFFF, value);
    }

    internal void DisposeBackup() => _backupDevice.Dispose();
}

internal sealed class GamePakRegion<TAccess>(GamePak gamePak) : IMemoryRegion where TAccess : struct, IAccess
{
    private readonly GamePak _gamePak = gamePak;


    public byte Read8(uint address, PipelineAccess access)    => _gamePak.Read8<TAccess>(address, access);
    public ushort Read16(uint address, PipelineAccess access) => _gamePak.Read16<TAccess>(address, access);
    public uint Read32(uint address, PipelineAccess access)   => _gamePak.Read32<TAccess>(address, access);

    public void Write8(uint address, PipelineAccess access, byte value)    => _gamePak.Write8<TAccess>(address, access, value);
    public void Write16(uint address, PipelineAccess access, ushort value) => _gamePak.Write16<TAccess>(address, access, value);
    public void Write32(uint address, PipelineAccess access, uint value)   => _gamePak.Write32<TAccess>(address, access, value);

    public void Dispose() => _gamePak.Dispose();
}

internal sealed class BackupRegion(GamePak gamePak) : IMemoryRegion
{
    private readonly GamePak _gamePak = gamePak;


    public byte Read8(uint address, PipelineAccess access)    => _gamePak.ReadBackup(address);
    public ushort Read16(uint address, PipelineAccess access) => (ushort)(_gamePak.ReadBackup(address) * 0x0101);
    public uint Read32(uint address, PipelineAccess access)   => (uint)(_gamePak.ReadBackup(address) * 0x01010101);

    public void Write8(uint address, PipelineAccess access, byte value)    => _gamePak.WriteBackup(address, value);
    public void Write16(uint address, PipelineAccess access, ushort value) => _gamePak.WriteBackup(address, (byte)(value >> ((int)(address & 1) << 3)));
    public void Write32(uint address, PipelineAccess access, uint value)   => _gamePak.WriteBackup(address, (byte)(value >> ((int)(address & 3) << 3)));

    public void Dispose() => _gamePak.DisposeBackup();
}