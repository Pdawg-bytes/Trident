using Trident.Core.Enums;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.GamePak
{
    internal partial class GamePak
    {
        private MemoryAccessHandler GetHandler<TAccess>() where TAccess : struct, IAccess
        {
            return new MemoryAccessHandler()
            {
                Read8 = Read8<TAccess>,
                Read16 = Read16<TAccess>,
                Read32 = Read32<TAccess>,

                Dispose = Dispose
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte Read8<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess
        {
            int alignShift = (int)(address & 1) << 3;
            return (byte)(ReadData16<TAccess>(address, IsSequential(access)) >> alignShift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort Read16<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess =>
            ReadData16<TAccess>(address, IsSequential(access));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Read32<TAccess>(uint address, PipelineAccess access) where TAccess : struct, IAccess =>
            ReadData32<TAccess>(address, IsSequential(access));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsSequential(PipelineAccess access) => (access & PipelineAccess.Sequential) == PipelineAccess.Sequential;


        private MemoryAccessHandler InitBackupHandler()
        {
            return new MemoryAccessHandler()
            {
                Read8 = ReadBackup8,
                Read16 = ReadBackup16,
                Read32 = ReadBackup32,

                Write8 = WriteBackup8,
                Write16 = WriteBackup16,
                Write32 = WriteBackup32,

                Dispose = _backupDevice.Dispose
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte ReadBackup8(uint address, PipelineAccess access)
        {
            // TODO: step scheduler based on waitstate settings
            return _backupDevice.Read(address);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort ReadBackup16(uint address, PipelineAccess access)
        {
            // TODO: step scheduler based on waitstate settings
            return (ushort)(_backupDevice.Read(address) * 0x0101);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint ReadBackup32(uint address, PipelineAccess access)
        {
            // TODO: step scheduler based on waitstate settings
            return (uint)(_backupDevice.Read(address) * 0x01010101);
        }

        private void WriteBackup8(uint address, PipelineAccess access, byte value)
        {
            // TODO: step scheduler based on waitstate settings
            _backupDevice.Write(address, value);
        }
        private void WriteBackup16(uint address, PipelineAccess access, ushort value)
        {
            // TODO: step scheduler based on waitstate settings
            _backupDevice.Write(address, (byte)(value >> ((ushort)(address & 1) << 3)));
        }
        private void WriteBackup32(uint address, PipelineAccess access, uint value)
        {
            // TODO: step scheduler based on waitstate settings
            _backupDevice.Write(address, (byte)(value >> ((int)(address & 3) << 3)));
        }
    }
}