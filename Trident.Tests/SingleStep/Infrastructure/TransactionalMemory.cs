using System.Runtime.CompilerServices;
using Trident.Core.Bus;
using Trident.Core.Enums;
using Trident.Tests.SingleStep.Models;

namespace Trident.Tests.SingleStep.Infrastructure
{
    internal struct TransactionalMemory : IDataBus
    {
        private uint _baseAddr;
        private uint _opcode;

        private List<Transaction> _transactions;
        private int _transactionIndex;

        internal void Initialize(uint baseAddr, uint opcode, List<Transaction> transactions)
        {
            _baseAddr = baseAddr;
            _opcode = opcode;
            _transactions = transactions;
            _transactionIndex = 0;
        }

        private Transaction? FindTransaction(uint address, int size, int kind)
        {
            while (_transactionIndex < _transactions.Count)
            {
                Transaction? tx = _transactions[_transactionIndex++];
                if (tx.Addr == address && tx.Size == size && tx.Kind == kind)
                    return tx;
            }

            return null;
        }

        private uint ReadTransactions(uint address, PipelineAccess access, int size)
        {
            if (_transactions == null) return 0;

            int kind = 0;
            if ((access & PipelineAccess.Code) != PipelineAccess.Code)
                kind = 1;

            Transaction? expected = FindTransaction(address, size, kind);
            return expected?.Data ?? throw new InvalidOperationException($"Unexpected read @ {address}");
        }

        private void WriteTransactions(uint address, PipelineAccess access, uint value, int size)
        {
            Transaction? expected = FindTransaction(address, size, kind: 2);
            if (expected == null || expected.Data != value)
                throw new InvalidOperationException($"Unexpected write @ {address} = {value}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Align<T>(uint value) where T : unmanaged =>
            value & ~(uint)(Unsafe.SizeOf<T>() - 1);

        public byte Read8(uint address, PipelineAccess access) => (byte)ReadTransactions(Align<byte>(address), access, size: 1);
        public ushort Read16(uint address, PipelineAccess access) => (ushort)ReadTransactions(Align<ushort>(address), access, size: 2);
        public uint Read32(uint address, PipelineAccess access) => ReadTransactions(Align<uint>(address), access, size: 4);

        public void Write8(uint address, PipelineAccess access, byte value) => WriteTransactions(Align<byte>(address), access, value, size: 1);
        public void Write16(uint address, PipelineAccess access, ushort value) => WriteTransactions(Align<ushort>(address), access, value, size: 2);
        public void Write32(uint address, PipelineAccess access, uint value) => WriteTransactions(Align<uint>(address), access, value, size: 4);
    }
}