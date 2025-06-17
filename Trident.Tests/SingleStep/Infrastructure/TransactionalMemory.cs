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
            if ((access & PipelineAccess.Code) == PipelineAccess.Code)
                return address == _baseAddr ? _opcode : address;

            Transaction? expected = FindTransaction(address, size, kind: 1);
            return expected?.Data ?? throw new InvalidOperationException($"Unexpected read @ 0x{address:X8}");
        }

        private void WriteTransactions(uint address, PipelineAccess access, uint value, int size)
        {
            Transaction? expected = FindTransaction(address, size, kind: 2);
            if (expected == null || expected.Data != value)
                throw new InvalidOperationException($"Unexpected write @ 0x{address:X8} = 0x{value:X8}");
        }

        public byte Read8(uint address, PipelineAccess access) => (byte)ReadTransactions(address, access, size: 1);
        public ushort Read16(uint address, PipelineAccess access) => (ushort)ReadTransactions(address, access, size: 2);
        public uint Read32(uint address, PipelineAccess access) => ReadTransactions(address, access, size: 4);

        public void Write8(uint address, PipelineAccess access, byte value) => WriteTransactions(address, access, value, size: 1);
        public void Write16(uint address, PipelineAccess access, ushort value) => WriteTransactions(address, access, value, size: 2);
        public void Write32(uint address, PipelineAccess access, uint value) => WriteTransactions(address, access, value, size: 4);
    }
}