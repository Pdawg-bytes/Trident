using Trident.Core.CPU;
using System.Runtime.CompilerServices;
using Trident.Core.Global;

namespace Trident.Core.Memory.GamePak;

internal sealed partial class GamePak
{
    internal sealed class GamePakRegion(GamePak gamePak, bool isLower) : MemoryBase(0, gamePak._step)
    {
        private readonly GamePak _gamePak = gamePak;
        private readonly bool _isLower    = isLower;

        public override uint BaseAddress => _gamePak.BaseAddress;
        public override uint Length      => _gamePak.Length;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte Read8(uint address, PipelineAccess access)
        {
            bool isSequential = IsSequential(address, access);
            uint shift        = (address & 1) << 3;
            _gamePak.WaitAccess16(address, isSequential);

            return (byte)(_gamePak.ReadData16(address, isSequential, _isLower) >> (int)shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ushort Read16(uint address, PipelineAccess access)
        {
            address = address.Align<ushort>();

            bool isSequential = IsSequential(address, access);
            _gamePak.WaitAccess16(address, isSequential);

            return _gamePak.ReadData16(address, isSequential, _isLower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override uint Read32(uint address, PipelineAccess access)
        {
            address = address.Align<uint>();

            bool isSequential = IsSequential(address, access);
            _gamePak.WaitAccess32(address, isSequential);

            return _gamePak.ReadData32(address, isSequential, _isLower);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write8(uint address, PipelineAccess access, byte value)
        {
            bool isSequential = IsSequential(address, access);
            _gamePak.WaitAccess16(address, isSequential);

            _gamePak.WriteData16(address, isSequential, (ushort)(value * 0x0101), _isLower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write16(uint address, PipelineAccess access, ushort value)
        {
            address = address.Align<ushort>();

            bool isSequential = IsSequential(address, access);
            _gamePak.WaitAccess16(address, isSequential);

            _gamePak.WriteData16(address, isSequential, value, _isLower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write32(uint address, PipelineAccess access, uint value)
        {
            address = address.Align<uint>();

            bool isSequential = IsSequential(address, access);
            _gamePak.WaitAccess32(address, isSequential);

            _gamePak.WriteData16(address | 0, isSequential, (ushort)value, _isLower);
            _gamePak.WriteData16(address | 2, true, (ushort)(value >> 16), _isLower);
        }


        public override void Dispose() => _gamePak.Dispose();
    }

    internal sealed class BackupRegion(GamePak gamePak) : MemoryBase(0, gamePak._step)
    {
        private readonly GamePak _gamePak = gamePak;

        public override uint BaseAddress => 0x0E000000;
        public override uint Length      => 64 * 1024;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte Read8(uint address, PipelineAccess access) => _gamePak.ReadBackup(address);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ushort Read16(uint address, PipelineAccess access)
        {
            byte b = _gamePak.ReadBackup(address);
            return (ushort)(b * 0x0101);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override uint Read32(uint address, PipelineAccess access)
        {
            byte b = _gamePak.ReadBackup(address);
            return (uint)(b * 0x01010101);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write8(uint address, PipelineAccess access, byte value) => _gamePak.WriteBackup(address, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write16(uint address, PipelineAccess access, ushort value)
        {
            byte extracted = (byte)(value >> ((int)(address & 1) << 3));
            _gamePak.WriteBackup(address, extracted);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write32(uint address, PipelineAccess access, uint value)
        {
            byte extracted = (byte)(value >> ((int)(address & 3) << 3));
            _gamePak.WriteBackup(address, extracted);
        }


        public override void Dispose() => _gamePak._backupDevice?.Dispose();
    }
}