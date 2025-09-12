using Trident.Core.CPU.Pipeline;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.GamePak
{
    internal partial class GamePak
    {
        private MemoryAccessHandler GetHandler<TAccess>() where TAccess : struct, IAccess => new
        (
            read8:  Read8<TAccess>,
            read16: Read16<TAccess>,
            read32: Read32<TAccess>,

            write8:  Write8<TAccess>,
            write16: Write16<TAccess>,
            write32: Write32<TAccess>,

            dispose: Dispose
        );


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


        private byte Read8<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
        {
            bool isSequential = IsSequential(address, access);

            WaitAccess16(address, isSequential);

            int shift = (int)(address & 1) << 3;
            return (byte)(ReadData16<TAccess>(address, isSequential) >> shift);
        }

        private ushort Read16<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
        {
            bool isSequential = IsSequential(address, access);

            WaitAccess16(address, isSequential);
            return ReadData16<TAccess>(address, isSequential);
        }

        private uint Read32<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
        {
            bool isSequential = IsSequential(address, access);

            WaitAccess32(address, isSequential);
            return ReadData32<TAccess>(address, isSequential);
        }


        private void Write8<TAccess>(uint address, PipelineAccess access, byte value) where TAccess : struct, IAccess =>
            Write16<TAccess>(address, access, (ushort)(value * 0x0101));

        private void Write16<TAccess>(uint address, PipelineAccess access, ushort value) where TAccess : struct, IAccess
        {
            bool isSequential = IsSequential(address, access);

            WaitAccess16(address, isSequential);
            WriteData16<TAccess>(address, isSequential, value);
        }

        private void Write32<TAccess>(uint address, PipelineAccess access, uint value) where TAccess : struct, IAccess
        {
            bool isSequential = IsSequential(address, access);

            WaitAccess32(address, isSequential);
            WriteData16<TAccess>(address | 0, isSequential, (ushort)value);
            WriteData16<TAccess>(address | 2, isSequential, (ushort)(value >> 16));
        }



        private MemoryAccessHandler InitBackupHandler() => new
        (
            read8:  (address, _) =>          ReadBackup(address),
            read16: (address, _) => (ushort)(ReadBackup(address) * 0x0101),
            read32: (address, _) => (uint)  (ReadBackup(address) * 0x01010101),

            write8:  (address, _, value) => WriteBackup(address, value),
            write16: (address, _, value) => WriteBackup(address, (byte)(value >> ((int)(address & 1) << 3))),
            write32: (address, _, value) => WriteBackup(address, (byte)(value >> ((int)(address & 3) << 3))),

            dispose: _backupDevice.Dispose
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte ReadBackup(uint address)
        {
            _step(_waitControl.AccessTimings16[0][3]);
            return _backupDevice.Read(address);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteBackup(uint address, byte value)
        {
            _step(_waitControl.AccessTimings16[0][3]);
            _backupDevice.Write(address & 0x0EFFFFFF, value);
        }
    }
}