using System.Runtime.CompilerServices;
using Trident.Core.Bus;
using Trident.Tests.SingleStep.Models;
using Trident.Core.CPU.Pipeline;

namespace Trident.Tests.SingleStep.Infrastructure;

internal struct TransactionalMemory : IDataBus
{
    private List<Transaction> _transactions;
    private int _transactionIndex;

    internal void Initialize(List<Transaction> transactions)
    {
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

        int kind = ((access & PipelineAccess.Code) == PipelineAccess.Code) ? 0 : 1;

        address = kind == 0
            ? size switch
            {
                2 => Align<ushort>(address),
                4 => Align<uint>(address),
                _ => address
            }
            : address;

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

    public byte Read8(uint address, PipelineAccess access) => (byte)ReadTransactions(address, access, size: 1);
    public ushort Read16(uint address, PipelineAccess access) => (ushort)ReadTransactions(address, access, size: 2);
    public uint Read32(uint address, PipelineAccess access) => ReadTransactions(address, access, size: 4);

    public void Write8(uint address, byte value, PipelineAccess access) => WriteTransactions(address, access, value, size: 1);
    public void Write16(uint address, ushort value, PipelineAccess access) => WriteTransactions(address, access, value, size: 2);
    public void Write32(uint address, uint value, PipelineAccess access) => WriteTransactions(address, access, value, size: 4);
}