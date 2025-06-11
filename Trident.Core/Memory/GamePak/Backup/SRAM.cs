using Trident.Core.Enums;
using System.Runtime.CompilerServices;

namespace Trident.Core.Memory.GamePak.Backup
{
    internal class SRAM : IBackupDevice
    {
        public uint Size => MEMORY_SIZE;
        public BackupType Type => BackupType.SRAM;

        internal const uint MEMORY_SIZE = 32 * 1024;
        private const uint ADDR_MASK = MEMORY_SIZE - 1;
        private UnsafeMemoryBlock _memory;

        internal SRAM(byte[]? saveData)
        {
            _memory = new(MEMORY_SIZE);
            if (saveData != null)
                _memory.WriteBytes(0, saveData);
        }

        public void Reset() => _memory.Clear();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Read(uint address) => _memory.Read8(address & ADDR_MASK);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint address, byte value) => _memory.Write8(address & ADDR_MASK, value);

        public void Dispose() => _memory.Dispose();
    }
}