using Trident.Core.Bus;
using System.Runtime.CompilerServices;

namespace Trident.Core.CPU.Decoding.Thumb;

internal partial struct ThumbDispatcher<TBus> where TBus : struct, IDataBus
{
    private const int ThumbDispatchCount = 0x400;
    private readonly ThumbInstruction[] _instructionHandlers = new ThumbInstruction[ThumbDispatchCount];

    private readonly ARM7TDMI<TBus> _cpu;

    internal ThumbDispatcher(ARM7TDMI<TBus> cpu)
    {
        _cpu = cpu;

        foreach (var i in Enumerable.Range(0, ThumbDispatchCount))
            _instructionHandlers[i] = _cpu.NonImplementedThumbInstr;

        InitGeneratedHandlers();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ThumbInstruction GetInstruction(ushort opcode) =>
        _instructionHandlers[opcode >> 6];
}